using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class ReinforceFlare : Skyfaller
    {
        private Effecter attachedEffecter;

        private Vector3 cachedDrawPos;

        private static List<int> mosquitosPassTracker = new List<int> { 360, };

        public float? overridePoint = null;

        public override Vector3 DrawPos
        {
            get
            {
                return cachedDrawPos;
            }
        }

        private float CurrentSpeed
        {
            get
            {
                if (def.skyfaller.speedCurve == null)
                {
                    return def.skyfaller.speed;
                }

                return def.skyfaller.speedCurve.Evaluate(TimeInAnimation) * def.skyfaller.speed;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            cachedDrawPos = base.DrawPos;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Spawned)
            {
                float dist = 0.17f * CurrentSpeed;
                Vector3 result = Vector3Utility.FromAngleFlat(angle - 90f) * dist;
                cachedDrawPos += result;
                if (attachedEffecter == null)
                {
                    attachedEffecter = GDDefOf.GDReinforceFlareAttached.SpawnAttached(this, MapHeld);
                    attachedEffecter.offset = DrawPos - Position.ToVector3Shifted();
                }
                attachedEffecter?.EffectTick(this, this);

                if (mosquitosPassTracker.Contains(ageTicks))
                {
                    DoReinforcement();
                }

                if (ageTicks == 800)
                {
                    if (RCellFinder.TryFindRandomPawnEntryCell(out var loc, Map, CellFinder.EdgeRoadChance_Hostile, allowFogged: false, (IntVec3 cell) => cell.Walkable(Map) && !cell.Fogged(Map) && !cell.Roofed(Map) && cell.GetEdifice(Map) == null && Map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors))))
                    {
                        IntVec3 siegeSpot = RCellFinder.FindSiegePositionFrom(loc, Map);
                        Messages.Message("GD.MosquitoReinforceArrival".Translate(), new TargetInfo(siegeSpot, Map), MessageTypeDefOf.NegativeEvent);
                        int num = Math.Min(Math.Max(9, (int)((overridePoint ?? StorytellerUtility.DefaultThreatPointsNow(Map)) / GDDefOf.Mech_Mosquito.combatPower * 0.5f)), 100);
                        GDUtility.SpawnMosquitosAt(Map, siegeSpot, num, 5);
                    }
                    else
                    {
                        IntVec3 siegeSpot = Map.AllCells.Where(c => !c.Fogged(Map) && c.Standable(Map) && !c.Roofed(Map)).RandomElementByWeight(c => c.DistanceToEdge(Map));
                        Messages.Message("GD.MosquitoReinforceArrival".Translate(), new TargetInfo(siegeSpot, Map), MessageTypeDefOf.NegativeEvent);
                        int num = Math.Min(Math.Max(9, (int)((overridePoint ?? StorytellerUtility.DefaultThreatPointsNow(Map)) / GDDefOf.Mech_Mosquito.combatPower * 0.5f)), 100);
                        GDUtility.SpawnMosquitosAt(Map, siegeSpot, num, 5);
                    }
                }
            }
            else
            {
                attachedEffecter?.Cleanup();
                attachedEffecter = null;
            }
        }

        public void DoReinforcement()
        {
            IntVec3 cell = Map.Center;
            ReinforceMosquito mosquito = (ReinforceMosquito)ThingMaker.MakeThing(GDDefOf.GD_ReinforceMosquito);
            GenPlace.TryPlaceThing(mosquito, cell, Map, ThingPlaceMode.Direct);
            GDDefOf.GDMosquitosPassing.PlayOneShotOnCamera(Map);
            Find.CameraDriver.shaker.DoShake(0.04f, 240);
            float numX = 2.2f;
            float numZ = 2.2f;
            int num2 = 6;
            for (int i = 1; i <= num2; i++)
            {
                Vector3 vec = i * new Vector3(numX, 0f, numZ);
                ReinforceMosquito mos = (ReinforceMosquito)ThingMaker.MakeThing(GDDefOf.GD_ReinforceMosquito);
                mos.adjustPos = vec;
                GenPlace.TryPlaceThing(mos, cell, Map, ThingPlaceMode.Direct);
            }
            for (int i = 1; i <= num2; i++)
            {
                Vector3 vec = i * new Vector3(numX, 0f, -numZ);
                ReinforceMosquito mos = (ReinforceMosquito)ThingMaker.MakeThing(GDDefOf.GD_ReinforceMosquito);
                mos.adjustPos = vec;
                GenPlace.TryPlaceThing(mos, cell, Map, ThingPlaceMode.Direct);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            GDUtility.DrawHighlightLineBetween(drawLoc, (drawLoc - Position.ToVector3Shifted()) * 0.3f + Position.ToVector3Shifted(), 1f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cachedDrawPos, "cachedDrawPos");
            Scribe_Values.Look(ref overridePoint, "overridePoint", null);
        }
    }
}
