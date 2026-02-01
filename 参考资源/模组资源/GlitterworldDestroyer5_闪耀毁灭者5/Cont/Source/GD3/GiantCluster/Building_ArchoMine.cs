using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using RimWorld;

namespace GD3
{
	[StaticConstructorOnStartup]
	public class Building_ArchoMine : Building, IPathFindCostProvider
	{
		private bool CanSetAutoRearm
		{
			get
			{
				return base.Faction == Faction.OfPlayer && this.def.blueprintDef != null && this.def.IsResearchFinished;
			}
		}

		private List<Pawn> cachedList;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<bool>(ref this.autoRearm, "autoRearm", false, false);
			Scribe_Values.Look(ref visibleNow, "visibleNow");
			Scribe_Collections.Look<Pawn>(ref this.touchingPawns, "testees", LookMode.Reference, Array.Empty<object>());
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (this.touchingPawns.RemoveAll((Pawn x) => x == null) != 0)
				{
					Log.Error("Removed null pawns from touchingPawns.");
				}
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				this.autoRearm = (this.CanSetAutoRearm && map.areaManager.Home[base.Position]);
				visibleNow = false;
			}
		}

		protected override void Tick()
		{
			if (base.Spawned)
			{
				if (this.IsHashIntervalTick(80))
                {
					ResetListPawns();
				}
				if (cachedList == null || !cachedList.Any())
				{
					ResetListPawns();
				}
				if (cachedList.Count > 0)
				{
					cachedList.SortBy((Pawn p) => p.Position.DistanceTo(this.Position));
					float dist = cachedList[0].Position.DistanceTo(this.Position);
					if (dist <= 3.9f)
					{
						visibleNow = true;
					}
				}
				List<Thing> thingList = base.Position.GetThingList(base.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					Pawn pawn = thingList[i] as Pawn;
					if (pawn != null && !this.touchingPawns.Contains(pawn))
					{
						this.touchingPawns.Add(pawn);
						this.CheckSpring(pawn);
					}
				}
				for (int j = 0; j < this.touchingPawns.Count; j++)
				{
					Pawn pawn2 = this.touchingPawns[j];
					if (pawn2 == null || !pawn2.Spawned || pawn2.Position != base.Position)
					{
						this.touchingPawns.Remove(pawn2);
					}
				}
			}
			base.Tick();
		}

		private void CheckSpring(Pawn p)
		{
			if (Rand.Chance(this.SpringChance(p)))
			{
				if (p.Faction == this.Faction)
                {
					return;
                }
				Map map = base.Map;
				this.Spring(p);
				//p.Kill(null);
				Messages.Message("ArchoMineTriggered".Translate(), MessageTypeDefOf.NegativeEvent);
				if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
				{
					Find.LetterStack.ReceiveLetter("LetterFriendlyTrapSprungLabel".Translate(p.LabelShort, p).CapitalizeFirst(), "LetterFriendlyTrapSprung".Translate(p.LabelShort, p).CapitalizeFirst(), LetterDefOf.NegativeEvent, new TargetInfo(base.Position, map, false), null, null, null, null);
				}
			}
		}

		protected virtual float SpringChance(Pawn p)
		{
			float num = 1f;
			if (this.KnowsOfTrap(p))
			{
				num = 0f;
			}
			num *= this.GetStatValue(StatDefOf.TrapSpringChance, true, -1) * p.GetStatValue(StatDefOf.PawnTrapSpringChance, true, -1);
			if (num <= 0.35f)
            {
				num = 0.35f;
            }
			return Mathf.Clamp01(num);
		}

		public bool KnowsOfTrap(Pawn p)
		{
			return (p.Faction != null && !p.Faction.HostileTo(base.Faction)) || (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState) || (p.guest != null && p.guest.Released) || (!p.IsPrisoner && base.Faction != null && p.HostFaction == base.Faction) || (p.RaceProps.Humanlike && p.IsFormingCaravan()) || (p.IsPrisoner && p.guest.ShouldWaitInsteadOfEscaping && base.Faction == p.HostFaction) || (p.Faction == null && p.RaceProps.Humanlike);
		}

		public override ushort PathWalkCostFor(Pawn p)
		{
			if (!this.KnowsOfTrap(p))
			{
				return 0;
			}
			return 800;
		}

		public ushort PathFindCostFor(Pawn p)
		{
			if (!KnowsOfTrap(p))
			{
				return 0;
			}
			return 800;
		}

		public override bool IsDangerousFor(Pawn p)
		{
			return this.KnowsOfTrap(p);
		}

		public void Spring(Pawn p)
		{
			this.SpringSub(p);
			if (this.def.building.trapDestroyOnSpring)
			{
				if (!base.Destroyed)
				{
					this.Destroy(DestroyMode.Vanish);
				}
			}
		}

		protected void SpringSub(Pawn p)
		{
			base.GetComp<CompExplosive>().StartWick(null);
		}

		private void ResetListPawns()
        {
			IEnumerable<Pawn> enumerable = from x in this.Map.mapPawns.AllPawnsSpawned
										   where x.Faction != null && x.Faction.HostileTo(this.Faction) && !x.Downed
										   select x;
			List<Pawn> list = enumerable.ToList();
			cachedList = list;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (Spawned)
			{
				if (visibleNow)
                {
					base.DrawAt(drawLoc, flip);
                }
			}
        }

        public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (DebugSettings.ShowDevGizmos)
            {
				yield return new Command_Action
				{
					defaultLabel = "DEV: visible?",
					action = delegate ()
                    {
						Log.Warning(visibleNow.ToString());
                    }
				};
			}
			yield break;
		}

		public CellRect GetOccupiedRect()
		{
			return this.OccupiedRect();
		}

		public bool visibleNow = false;

		private bool autoRearm;

		private List<Pawn> touchingPawns = new List<Pawn>();
	}
}
