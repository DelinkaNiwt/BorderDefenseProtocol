using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.SketchGen;

namespace GD3
{
    internal class IncidentWorker_MechClusterGiant : IncidentWorker
    {
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			if (!ModsConfig.RoyaltyActive)
			{
				return false;
			}
			if (!base.CanFireNowSub(parms))
			{
				return false;
			}
			return Faction.OfMechanoids != null;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
            MechClusterSketch sketch = this.GenerateClusterSketch(parms.points, map);
			IntVec3 center = MechClusterUtility.FindClusterPosition(map, sketch, 100, 0.5f);
			if (!center.IsValid)
			{
				return false;
			}
			IEnumerable<Thing> targets = from t in MechClusterUtility.SpawnCluster(center, map, sketch, dropInPods: true, canAssaultColony: true, parms.questTag)
										 where t.def != ThingDefOf.Wall && t.def != ThingDefOf.Barricade
										 select t;
			SendStandardLetter(parms, new LookTargets(targets));
			return true;
		}

        public MechClusterSketch GenerateClusterSketch(float points, Map map, bool startDormant = true, bool forceNoConditionCauser = false)
        {
            if (!ModLister.CheckRoyalty("Mech cluster") || !ModsConfig.RoyaltyActive)
            {
                return new MechClusterSketch(new Sketch(), new List<MechClusterSketch.Mech>(), startDormant);
            }

            points = Mathf.Min(points, 10000f);
            float num = points;
            List<MechClusterSketch.Mech> list = null;
            if (Rand.Chance(MechClusterGenerator.PointsToPawnsChanceCurve.Evaluate(points)))
            {
                List<PawnKindDef> source = DefDatabase<PawnKindDef>.AllDefsListForReading.Where(MechClusterGenerator.MechKindSuitableForCluster).ToList();
                list = new List<MechClusterSketch.Mech>();
                float a = Rand.ByCurve(MechClusterGenerator.PawnPointsRandomPercentOfTotalCurve) * num;
                float pawnPointsLeft;
                a = (pawnPointsLeft = Mathf.Max(a, source.Min((PawnKindDef x) => x.combatPower)));
                PawnKindDef result;
                while (pawnPointsLeft > 0f && source.Where((PawnKindDef def) => def.combatPower <= pawnPointsLeft).TryRandomElement(out result))
                {
                    pawnPointsLeft -= result.combatPower;
                    list.Add(new MechClusterSketch.Mech(result));
                }

                num -= a - pawnPointsLeft;
            }

            Sketch buildingsSketch = SketchGen.Generate(GDDefOf.MechCluster_Giant, new SketchResolveParams
            {
                points = num,
                totalPoints = points,
                mechClusterDormant = startDormant,
                sketch = new Sketch(),
                mechClusterForMap = map,
                forceNoConditionCauser = forceNoConditionCauser
            });
            if (list != null)
            {
                List<IntVec3> pawnUsedSpots = new List<IntVec3>();
                for (int i = 0; i < list.Count; i++)
                {
                    MechClusterSketch.Mech pawn = list[i];
                    if (!buildingsSketch.OccupiedRect.Where((IntVec3 c) => !buildingsSketch.ThingsAt(c).Any() && !pawnUsedSpots.Contains(c)).TryRandomElement(out IntVec3 result2))
                    {
                        CellRect cellRect = buildingsSketch.OccupiedRect;
                        do
                        {
                            cellRect = cellRect.ExpandedBy(1);
                        }
                        while (!cellRect.Where((IntVec3 x) => !buildingsSketch.WouldCollide(pawn.kindDef.race, x, Rot4.North) && !pawnUsedSpots.Contains(x)).TryRandomElement(out result2));
                    }

                    pawnUsedSpots.Add(result2);
                    pawn.position = result2;
                    list[i] = pawn;
                }
            }

            return new MechClusterSketch(buildingsSketch, list, startDormant);
        }
    }
}
