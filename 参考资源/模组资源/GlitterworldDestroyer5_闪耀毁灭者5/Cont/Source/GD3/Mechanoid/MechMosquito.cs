using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class MechMosquito : Pawn
    {
        public IntVec3 dest;

        public int airRaidTicker = -1;

        public List<IntVec3> cells = new List<IntVec3>();

        protected override void Tick()
        {
            base.Tick();
            Map map = MapHeld;
            if (airRaidTicker >= 0)
            {
                airRaidTicker++;
                int num = cells.Count;
                int index = airRaidTicker / 3;
                if (index > num - 1)
                {
                    Reset();
                }
                else if (airRaidTicker % 3 == 0)
                {
                    CastShoot(cells[index], map);
                }
            }
            if (this.IsHashIntervalTick(12) && Flying)
            {
                for (int i = 0; i < 3; i++)
                {
                    ThrowFleck(DrawPos + GDUtility.RandomPointInCircle(0.2f) + GetPos(), map, FleckDefOf.MicroSparksFast, 0.25f, GetAngle());
                    ThrowFleck(DrawPos + GDUtility.RandomPointInCircle(0.5f) + GetPos(), map, FleckDefOf.FireGlow, 0.5f, GetAngle());
                }
            }
        }

        public override void Notify_Downed()
        {
            base.Notify_Downed();
            Reset();
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (Flying && !dinfo.Def.isRanged && !dinfo.Def.isExplosive && dinfo.Def != DamageDefOf.EMP)
            {
                Pawn pawn = dinfo.Instigator as Pawn;
                if (pawn != null && pawn.Flying)
                {
                    return;
                }
                absorbed = true;
            }
        }

        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            base.Kill(dinfo, exactCulprit);
            Reset();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            Reset();
        }

        public void Reset()
        {
            dest = IntVec3.Invalid;
            airRaidTicker = -1;
            cells.Clear();
            if (CurJobDef == JobDefOf.Wait_Combat)
            {
                jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        }

        public void CastShoot(IntVec3 targ, Map map)
        {
            Projectile projectile = (Projectile)GenSpawn.Spawn(GDDefOf.Bullet_MosquitoChargeLance, PositionHeld, map);
            projectile.Launch(this, DrawPos, targ + GenRadial.RadialPattern[Rand.Range(0, 1)], mindState.enemyTarget, ProjectileHitFlags.All, false, null);
            GDDefOf.ChargeLance_Fire.PlayOneShot(this);
        }

        public float GetAngle()
        {
            if (Rotation == Rot4.South) return 0;
            else if (Rotation == Rot4.West) return 90;
            else if (Rotation == Rot4.East) return 270;
            return 180;
        }

        public Vector3 GetPos()
        {
            if (Rotation == Rot4.South) return new Vector3(0,0,0.6f);
            else if (Rotation == Rot4.West) return new Vector3(0.6f, 0, 0);
            else if (Rotation == Rot4.East) return new Vector3(-0.6f, 0, 0);
            return new Vector3(0, 0, -0.6f);
        }

        public void ThrowFleck(Vector3 loc, Map map, FleckDef def, float size, float angle)
        {
            if (loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, def, Rand.Range(1.5f, 2.5f) * size);
                dataStatic.rotationRate = Rand.Range(-30f, 30f);
                dataStatic.velocityAngle = angle;
                dataStatic.velocitySpeed = Rand.Range(0.5f, 1.0f);
                map.flecks.CreateFleck(dataStatic);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref airRaidTicker, "airRaidTicker", -1);
            Scribe_Values.Look(ref dest, "dest");
            Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
        }
    }
}
