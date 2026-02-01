using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
	public class CompBlackComputer : ThingComp
	{
        public int enemyTick = -1;

        public override void CompTick()
        {
            if (enemyTick > 0 && Find.TickManager.TicksGame == enemyTick)
            {
                Map map = parent.Map;
                if (RCellFinder.TryFindRandomPawnEntryCell(out var loc, map, CellFinder.EdgeRoadChance_Hostile, allowFogged: false, (IntVec3 cell) => cell.Walkable(map) && !cell.Fogged(map) && !cell.Roofed(map) && cell.GetEdifice(map) == null && map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.NoPassClosedDoors))))
                {
                    IntVec3 siegeSpot = RCellFinder.FindSiegePositionFrom(loc, map);
                    PlaceComet(GDDefOf.Mech_BlackScyther, siegeSpot, 3.9f);
                    PlaceComet(GDDefOf.Mech_BlackLancer, siegeSpot, 3.9f);
                    Find.LetterStack.ReceiveLetter("GD.BlackMechAlert".Translate(), "GD.BlackMechAlertDesc".Translate(), LetterDefOf.ThreatSmall, new TargetInfo(siegeSpot, map));
                }
            }
        }

        public override void Notify_Hacked(Pawn hacker = null)
		{
			GDUtility.SendSignal(GDUtility.GetQuestOfThing(parent), "Hacked");
            enemyTick = Find.TickManager.TicksGame + 300;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
			CompHackable comp = parent.TryGetComp<CompHackable>();
            if (comp == null || !comp.IsHacked)
            {
                GDUtility.SendSignal(GDUtility.GetQuestOfSite(previousMap.Parent as Site), "Lost");
            }
        }

        private void PlaceComet(PawnKindDef pawnKindDef, IntVec3 center, float radius)
        {
            ThingWithComps comet = (ThingWithComps)ThingMaker.MakeThing(GDDefOf.BlackStrike_Pod);
            CompSpawnThingOnDestroy comp = comet.TryGetComp<CompSpawnThingOnDestroy>();
            comp.pawnKindDef = pawnKindDef;
            comp.faction = GDDefOf.BlackMechanoid;
            IntVec3 nextExplosionCell = GenRadial.RadialCellsAround(center, radius, true).Where(c => c.Standable(parent.Map) && !c.Fogged(parent.Map) && parent.Map.reachability.CanReach(c, parent.Map.Center, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassAllDestroyableThings))).RandomElement();
            if (nextExplosionCell.IsValid)
            {
                GenPlace.TryPlaceThing(comet, nextExplosionCell, parent.Map, ThingPlaceMode.Direct);
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref enemyTick, "enemyTick", -1);
        }
    }

}
