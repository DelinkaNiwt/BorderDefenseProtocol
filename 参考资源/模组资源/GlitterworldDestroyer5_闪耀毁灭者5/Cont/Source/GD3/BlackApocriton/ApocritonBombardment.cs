using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
    public class ApocritonBombardment : ThingWithComps
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.spawnTick, "spawnTick", 0, false);
            Scribe_Values.Look<float>(ref this.impactAreaRadius, "impactAreaRadius", 15.9f, false);
        }
        protected override void Tick()
        {
            base.Tick();
            if (spawnTick > 600)
            {
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                Map map = this.Map;
                if (Spawned)
                {
                    if (spawnTick % 8 == 0 && spawnTick != 0)
                    {
                        StartRandomFire();
                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.BlackStrike, null), nextExplosionCell, this.Map, ThingPlaceMode.Near, null, null, default(Rot4));
                    }
                    this.spawnTick++;
                }
            }
        }
        private void StartRandomFire()
        {
            nextExplosionCell = (from x in GenRadial.RadialCellsAround(base.Position, impactAreaRadius, useCenter: true)
                                 where x.InBounds(base.Map)
                                 select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / impactAreaRadius));
        }

        private int spawnTick = 0;

        private float impactAreaRadius = 15.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}
