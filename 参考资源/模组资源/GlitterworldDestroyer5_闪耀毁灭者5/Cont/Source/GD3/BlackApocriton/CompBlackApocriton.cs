using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse.Sound;
using RimWorld.Planet;

namespace GD3
{
	public class CompBlackApocriton : ThingComp, IRoofCollapseAlert
	{
		public Pawn Apocriton
        {
            get
            {
				return this.parent as Pawn;
            }
        }

		public CompProperties_BlackApocriton Props
		{
			get
			{
				return (CompProperties_BlackApocriton)this.props;
			}
		}

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
			absorbed = false;
			if (inMission == true)
            {
				MoteMaker.ThrowText(Apocriton.Position.ToVector3(), Apocriton.Map, "Apocriton_DamageBlocked".Translate(), 4f);
				FleckMaker.Static(Apocriton.Position.ToVector3(), Apocriton.Map, GDDefOf.ApocritonResurrectFlashGrowing, 2.0f);

				Find.World.GetComponent<MissionComponent>().BlackMechRelationOffset(-100);
				Messages.Message("GD.DamageBlack".Translate(100), MessageTypeDefOf.NeutralEvent);

				List<Pawn> pawns = Apocriton.Map.mapPawns.AllPawns;
				for (int i = 0; i < pawns.Count; i++)
                {
					Pawn p = pawns[i];
					if (p != Apocriton)
                    {
						p.DeSpawn(DestroyMode.KillFinalize);
                    }
                }
				PocketMapUtility.DestroyPocketMap(Find.Maps.First(m => m.Biome.defName == "DryOcean"));

				absorbed = true;
			}
		} 

        public override void CompTick()
		{
			base.CompTick();
			Pawn pawn = this.Apocriton;
			bool flag = pawn != null && pawn.Spawned;
			if (inMission)
            {
				List<Pawn> pawns = Apocriton.Map.mapPawns.AllPawns.FindAll(p => p.Faction != null && p.Faction == Faction.OfPlayer);
				if (pawns.Count == 0)
                {
					return;
                }
				if (!communicate)
                {
					Pawn p = pawns[0];
					if (p.Position.DistanceTo(Apocriton.Position) <= 2.9f)
                    {
						communicate = true;
						string sig = Find.World.GetComponent<MissionComponent>().BranchDict.TryGetValue("WillMilitorDie", true) ? "CommDie" : "CommLive";
						QuestUtility.SendQuestTargetSignals(Apocriton.questTags, sig, Apocriton.Named("SUBJECT"));
					}
                }
				else
                {
					questTicker++;
					if (questTicker >= 0)
                    {
						QuestUtility.SendQuestTargetSignals(Apocriton.questTags, "Complete", Apocriton.Named("SUBJECT"));
					}
                }
				return;
            }
		}

		public RoofCollapseResponse Notify_OnBeforeRoofCollapse()
		{
			if (!Apocriton.DeadOrDowned)
			{
				if (RCellFinder.TryFindRandomCellNearWith(parent.Position, (IntVec3 c) => IsValidCell(c, parent.MapHeld), parent.MapHeld, out var result, 60))
				{
					SkipUtility.SkipTo(parent, result, parent.MapHeld);
				}
			}
			return RoofCollapseResponse.RemoveThing;
		}

		private static bool IsValidCell(IntVec3 cell, Map map)
		{
			if (cell.InBounds(map))
			{
				return cell.Walkable(map);
			}
			return false;
		}

		public override void PostExposeData()
		{
			Scribe_Values.Look<bool>(ref this.inMission, "inMission", false, false);
			Scribe_Values.Look<bool>(ref this.communicate, "communicate", false, false);
		}

		public bool inMission = false;

		private bool communicate;

		private int questTicker;
	}
}
