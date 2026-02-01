using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompSuppressionMarrow : ThingComp
	{
		public CompProperties_SuppressionMarrow Props
		{
			get
			{
				return (CompProperties_SuppressionMarrow)this.props;
			}
		}

		public List<Pawn> Pawns
		{
			get
			{
				List<Pawn> pawns = this.parent.Map.mapPawns.AllPawns;
				return pawns;
			}
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			base.PostDestroy(mode, previousMap);
			Messages.Message("MarrowBDestroy".Translate(), MessageTypeDefOf.NeutralEvent);
			float proj = Find.ResearchManager.GetProgress(GDDefOf.GD3_GiantCluster_Large);
			if (proj >= GDDefOf.GD3_GiantCluster_Large.baseCost)
			{
				return;
			}
			Messages.Message("TechBFinished".Translate(), MessageTypeDefOf.NeutralEvent);
			Find.ResearchManager.FinishProject(GDDefOf.GD3_GiantCluster_Large);

			GenExplosion.DoExplosion(parent.TrueCenter().ToIntVec3(), previousMap, -1, GDDefOf.MechBandShockwave, null, -1, -1, GDDefOf.Explosion_MechBandShockwave, null, null, null, null, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
			List<Building> buildings = previousMap.listerBuildings.allBuildingsNonColonist.FindAll(b => b is Building_TurretGun);
			List<Building> mines = previousMap.listerBuildings.allBuildingsNonColonist.FindAll(b => b is Building_ArchoMine);
			foreach (Building building in buildings)
			{
				building.TakeDamage(new DamageInfo(DamageDefOf.EMP, 100));
			}
			foreach (Building_ArchoMine mine in mines)
			{
				mine.visibleNow = true;
			}
			Site site = (Site)previousMap.Parent;
			List<Quest> quests = Find.QuestManager.QuestsListForReading;
			for (int j = 0; j < quests.Count; j++)
			{
				Quest quest = quests[j];
				if (!quest.hidden && !quest.Historical && !quest.dismissed && quest.QuestLookTargets.Contains(site))
				{
					if (quest.root.defName == "GD_Quest_Cluster_L")
                    {
						quest.End(QuestEndOutcome.Success);
					}
				}
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			Building building = this.parent as Building;
			Map map = building.Map;
			bool flag = building != null && map != null && Pawns.Count > 0;
			if (flag)
			{
				this.readyToUseTicks++;
				if (readyToUseTicks >= 60)
				{
					this.readyToUseTicks = 0;
					FleckMaker.Static(building.Position, map, FleckDefOf.PsycastAreaEffect, 8.9f);
					for (int i = 0; i < Pawns.Count; i++)
					{
						Pawn pawn = Pawns[i];
						if (pawn.Faction == null || (pawn.Faction != null && !pawn.Faction.HostileTo(building.Faction)))
						{
							continue;
						}
						AddRadioHediff(pawn, GDDefOf.PsychicSuppressionMarrowHediff);
					}
				}
			}
		}

		private void AddRadioHediff(Pawn pawn, HediffDef hediffDef)
		{
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
			if (hediff == null)
			{
				hediff = pawn.health.AddHediff(hediffDef, pawn.health.hediffSet.GetBrain(), null, null);
				hediff.Severity = 1f;
			}
			HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = 61;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
		}

		private int readyToUseTicks = 0;

	}
}