using System;
using Verse;
using RimWorld;
using System.Linq;

namespace GD3
{
    public class BlackBomb : ThingWithComps
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.spawnTick, "spawnTick", 0, false);
            Scribe_Values.Look<float>(ref this.impactAreaRadius, "impactAreaRadius", 5.9f, false);
        }
        protected override void Tick()
        {
            base.Tick();
            if (spawnTick > 76)
            {
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                if (spawnTick < 61)
                {
                    this.spawnTick++;
                    return;
                }
                Map map = this.Map;
                if (Spawned)
                {
                    if (spawnTick % 2 == 0 && spawnTick != 0)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            StartRandomFire();
                            GenExplosion.DoExplosion(nextExplosionCell, map, 3.9f, DamageDefOf.Bomb, null, 40, 0.8f, null, GDDefOf.Gun_ExplodeFloatingTurret, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                        }
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

        private float impactAreaRadius = 6.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}