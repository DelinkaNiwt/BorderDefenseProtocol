using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace GD3
{
    public class Projectile_Exostrider_Sub : Projectile
    {
        protected override void Tick()
        {
            base.Tick();
            List<IntVec3> vecs = new List<IntVec3>() { new IntVec3(-2, 0, 3), new IntVec3(2, 0, 3) };
            IntVec3 vec = this.Position + vecs.RandomElement();
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.ExostriderShell_Up), vec, Map, ThingPlaceMode.Direct);
            StartRandomFire();
            IntVec3 vec2 = nextExplosionCell;
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.ExostriderShell_Down), vec2, Map, ThingPlaceMode.Direct);
            Destroy();
        }

        private void StartRandomFire()
        {
            nextExplosionCell = (from x in GenRadial.RadialCellsAround(intendedTarget.CenterVector3.ToIntVec3(), impactAreaRadius, useCenter: true)
                                 where x.InBounds(base.Map)
                                 select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / impactAreaRadius));
        }

        private float impactAreaRadius = 2.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}
