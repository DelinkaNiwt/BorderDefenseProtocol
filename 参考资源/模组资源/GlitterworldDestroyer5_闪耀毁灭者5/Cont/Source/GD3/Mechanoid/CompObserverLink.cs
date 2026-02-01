using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	public class CompProperties_ObserverLink : CompProperties
	{
		public CompProperties_ObserverLink()
		{
			this.compClass = typeof(CompObserverLink);
		}

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleIconPath;

		public HediffDef hediff;

		public float range = 8.9f;
	}

	public class CompObserverLink : ThingComp
	{
		public CompProperties_ObserverLink Props
		{
			get
			{
				return this.props as CompProperties_ObserverLink;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			this.readyToUseTicks++;
			if (this.tmpPawns.Count > 0 && this.readyToUseTicks > 63)
			{
				if (!tmpPawns[0].Spawned || tmpPawns[0].Dead)
                {
					MoteMaker.ThrowText(this.parent.DrawPos, this.parent.Map, "GD.ObserverDisconnected".Translate(), 5f);
					SoundDefOf.DisconnectedMech.PlayOneShot(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
					this.tmpPawns.Clear();
					return;
				}
				this.readyToUseTicks = 0;
				Pawn p = this.tmpPawns[0];
				Hediff hediffToAdd = p.health?.hediffSet?.GetFirstHediffOfDef(Props.hediff);
				bool flag = (p.RaceProps.IsMechanoid || p.RaceProps.Humanlike) && p.Faction != null && this.parent.Faction != null && p.Faction == this.parent.Faction;
				if (flag)
				{
					if (hediffToAdd != null)
					{
						HediffComp_Disappears hediffComp_Disappears = hediffToAdd.TryGetComp<HediffComp_Disappears>();
						if (hediffComp_Disappears != null)
						{
							hediffComp_Disappears.ticksToDisappear = 90;
						}
					}
					else
					{
						Hediff hediff = HediffMaker.MakeHediff(this.Props.hediff, p);
						HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
						if (hediffComp_Disappears != null)
						{
							hediffComp_Disappears.ticksToDisappear = 90;
						}
						p.health.AddHediff(hediff);
					}
					hediffToAdd = p.health?.hediffSet?.GetFirstHediffOfDef(Props.hediff);
					if (hediffToAdd == null)
                    {
						return;
                    }
					bool flag2 = this.parent.Position.DistanceTo(p.Position) <= Props.range;
					if (flag2)
                    {
						hediffToAdd.Severity = 1.0f;
					}
                    else
                    {
						hediffToAdd.Severity = 0.5f;
					}
				}
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.parent.Faction != null && this.parent.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Action select = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						Find.Targeter.BeginTargeting(this.ConnectTargetParameters(), new Action<LocalTargetInfo>(this.StartConnect), new Action<LocalTargetInfo>(this.Highlight), new Func<LocalTargetInfo, bool>(this.CanConnect), null, null, null, true, null);
					}
				};
				if (((Pawn)this.parent).Downed)
				{
					select.Disable("GD.ObserverDowned".Translate());
				}
				if (MechanitorUtility.GetOverseer(((Pawn)this.parent)) == null)
                {
					select.Disable("GD.ObserverNotControled".Translate());
				}
				yield return select;
			}
			yield break;
		}

		public TargetingParameters ConnectTargetParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = true,
				canTargetBuildings = false,
				canTargetHumans = true,
				canTargetMechs = true,
				canTargetAnimals = false,
				canTargetLocations = false,
				validator = ((TargetInfo x) => x.HasThing && x.Thing.Faction != null && x.Thing.Faction == this.parent.Faction)
			};
		}

		public void StartConnect(LocalTargetInfo target)
        {
			if (this.tmpPawns.Count > 0 && this.tmpPawns[0] == (Pawn)target.Thing)
            {
				MoteMaker.ThrowText(this.parent.DrawPos, this.parent.Map, "GD.ObserverDisconnected".Translate(), 5f);
				SoundDefOf.DisconnectedMech.PlayOneShot(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
				this.tmpPawns.Clear();
				return;
            }
			this.tmpPawns.Clear();
			this.tmpPawns.Add((Pawn)target.Thing);
			if (this.tmpPawns.Count > 0)
            {
				MoteMaker.ThrowText(this.parent.DrawPos, this.parent.Map, "GD.ObserverConnected".Translate(string.Format("{0}", this.tmpPawns[0].Name)), 5f);
				SoundDefOf.ControlMech_Complete.PlayOneShot(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
			}
        }

		private void Highlight(LocalTargetInfo target)
		{
			bool isValid = target.IsValid;
			if (isValid)
			{
				GenDraw.DrawTargetHighlight(target);
			}
		}

		public bool CanConnect(LocalTargetInfo target)
		{
			Pawn pawn = target.Pawn;
			return pawn != null && this.parent.Faction != null && pawn.Faction != null && this.parent.Faction == pawn.Faction;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Collections.Look<Pawn>(ref this.tmpPawns, "tmpPawns", LookMode.Reference, Array.Empty<object>());
		}

		public override void PostDraw()
		{
			if (Find.Selector.SelectedObjectsListForReading.Contains(this.parent))
			{
				if (this.tmpPawns.Count > 0)
				{
					GenDraw.DrawLineBetween(this.parent.DrawPos, this.tmpPawns[0].DrawPos);
				}
			}
		}

		private int readyToUseTicks;

		public List<Pawn> tmpPawns = new List<Pawn>();
	}
}