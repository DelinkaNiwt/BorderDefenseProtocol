using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
    public class AlphaBombardment : ThingWithComps
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.spawnTick, "spawnTick", 0, false);
            Scribe_Values.Look<float>(ref this.impactAreaRadius, "impactAreaRadius", 14.9f, false);
        }
        protected override void Tick()
        {
            base.Tick();
            if (spawnTick > 400)
            {
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                Map map = this.Map;
                if (map == null)
                {
                    Destroy(DestroyMode.Vanish);
                }
                if (Spawned)
                {
                    if (spawnTick % 7 == 0 && spawnTick != 0)
                    {
                        //GDDefOf.PocketThunderEffect.Spawn(this.Position, this.Map, 1f).EffectTick(new TargetInfo(this.Position, this.Map, false), new TargetInfo(this.Position, this.Map, false));
                        StartRandomFire();
                        AlphaSkyfaller missle = (AlphaSkyfaller)ThingMaker.MakeThing(GDDefOf.AlphaStrike, null);
                        missle.instigator = this.instigator;
                        missle.equipment = this.instigator?.equipment?.Primary?.def;
                        GenPlace.TryPlaceThing(missle, nextExplosionCell, this.Map, ThingPlaceMode.Direct, null, null, default(Rot4));
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

        private float impactAreaRadius = 18.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public Pawn instigator;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}
