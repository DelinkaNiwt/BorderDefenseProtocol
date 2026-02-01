using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace GD3
{
    public class Projectile_Exostrider : Projectile
    {
        protected override void Tick()
        {
            base.Tick();
            IntVec3 vec = this.intendedTarget.CenterVector3.ToIntVec3();
            Map map = Map;
            IEnumerable<Pawn> enumerable = from x in Map.mapPawns.AllPawns
                                           where x.Position.DistanceTo(vec) < 9.9f && (x.Faction == null || x.Faction != null && x.Faction.HostileTo(launcher.Faction))
                                           select x;
            List<Pawn> pawns = enumerable.ToList();
            for (int j = 0; j < pawns.Count; j++)
            {
                Pawn pawn = pawns[j];
                if (pawn.def == GDDefOf.Mech_BlackApocriton)
                {
                    continue;
                }
                pawn.Destroy(DestroyMode.KillFinalize);
            }
            for (int i = 0; i < 2; i++)
            {
                StartRandomFire();
                GenExplosion.DoExplosion(nextExplosionCell, map, 9.9f, GDDefOf.BombSuper, launcher, 32500, 300f, null, GDDefOf.Artillery_Exostrider, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
            }
            GenExplosion.DoExplosion(vec, map, 9.9f, GDDefOf.BombSuper, launcher, 32500, 300f, null, GDDefOf.Artillery_Exostrider, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
            GDDefOf.GiantExplosion.Spawn(vec, map, 1f);
            GDDefOf.GD_BigWave.SpawnMaintained(vec, map, 0.75f);
            Find.CameraDriver.shaker.DoShake(30f);
            Destroy();
        }

        private void StartRandomFire()
        {
            nextExplosionCell = (from x in GenRadial.RadialCellsAround(intendedTarget.CenterVector3.ToIntVec3(), impactAreaRadius, useCenter: true)
                                 where x.InBounds(base.Map)
                                 select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / impactAreaRadius));
        }

        private float impactAreaRadius = 3.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}
