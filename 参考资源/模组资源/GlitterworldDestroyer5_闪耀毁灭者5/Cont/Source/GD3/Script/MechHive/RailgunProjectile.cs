using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class RailgunProjectile : Bullet
    {
        public virtual int ProjLife => 90;

        private Sustainer ambientSustainer;

        private Vector3 stringVec;

        public List<Thing> thingsDamaged;

        public override int UpdateRateTicks => 1;

        public override Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (destination - origin).Yto0().normalized * def.projectile.SpeedTilesPerTick * (ticksToImpact - lifetime);
                return origin.Yto0() + b + Vector3.up * def.Altitude;
            }
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
            }
            destination = this.intendedTarget.Thing.DrawPos;
            stringVec = origin;
            thingsDamaged = new List<Thing>();
        }

        protected override void TickInterval(int delta)
        {
            lifetime -= delta;

            int a = ticksToImpact - lifetime;
            if (!ExactPosition.InBounds(base.Map) || a > ProjLife)
            {
                for (int i = 0; i < 10; i++)
                {
                    FleckMaker.ThrowAirPuffUp(DrawPos, MapHeld);
                }
                Destroy();
                return;
            }

            Vector3 targ = stringVec;
            if (a >= 15)
            {
                Vector3 b = -(destination - origin).Yto0().normalized * def.projectile.SpeedTilesPerTick * 15;
                targ = ExactPosition + b;
            }
            stringVec = targ;

            base.Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            for (int i = 0; i < 4; i++)
            {
                ThrowSmoke(DrawPos + GDUtility.RandomPointInCircle(0.5f), Map, 0.3f, (destination - origin).Yto0().AngleFlat() + 180f);
            }

            List<Thing> things = MapHeld.spawnedThings.ToList().FindAll(t => t.Position.DistanceTo(Position) <= 1.9f);
            if (launcher != null)
            {
                things.RemoveAll(t => t.Faction != null && !t.HostileTo(launcher));
            }
            if (!things.NullOrEmpty())
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (!thingsDamaged.Contains(thing) && (thing is Pawn || thing is Building))
                    {
                        thingsDamaged.Add(thing);
                        Impact(thing);
                        if (thing is Building b && b.def.passability == Traversability.Impassable)
                        {
                            if (!Destroyed) Destroy();
                        }
                    }
                }
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            NotifyImpact(hitThing, map, position);
            if (hitThing != null)
            {
                Pawn pawn;
                bool instigatorGuilty = (pawn = (launcher as Pawn)) == null || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                dinfo.SetWeaponQuality(equipmentQuality);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn2 = hitThing as Pawn;
                pawn2?.stances?.stagger.Notify_BulletImpact(this);
                if (def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }

                if (Rand.Chance(base.DamageDef.igniteCellChance) && (pawn2 == null || Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(pawn2))))
                {
                    hitThing.TryAttachFire(Rand.Range(0.55f, 0.85f), launcher);
                }

                return;
            }

            if (!blockedByShield)
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map));
                if (base.Position.GetTerrain(map).takeSplashes)
                {
                    FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
                }
                else
                {
                    FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
                }
            }

            if (Rand.Chance(base.DamageDef.igniteCellChance))
            {
                FireUtility.TryStartFireIn(base.Position, map, Rand.Range(0.55f, 0.85f), launcher);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            GDUtility.DrawProjHighlightLineBetween(drawLoc, stringVec, 1f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stringVec, "stringVec");
            Scribe_Collections.Look(ref thingsDamaged, "thingsDamaged", LookMode.Reference);
        }

        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            if (this.Destroyed)
            {
                return;
            }
            BulletImpactData bulletImpactData = default(BulletImpactData);
            bulletImpactData.bullet = this;
            bulletImpactData.hitThing = hitThing;
            bulletImpactData.impactPosition = position;
            BulletImpactData impactData = bulletImpactData;
            hitThing?.Notify_BulletImpactNearby(impactData);
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }

                List<Thing> thingList = c.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != hitThing)
                    {
                        thingList[j].Notify_BulletImpactNearby(impactData);
                    }
                }
            }
        }

        public void ThrowSmoke(Vector3 loc, Map map, float size, float angle)
        {
            if (loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, FleckDefOf.Smoke, Rand.Range(1.5f, 2.5f) * size);
                dataStatic.rotationRate = Rand.Range(-30f, 30f);
                dataStatic.velocityAngle = angle;
                dataStatic.velocitySpeed = Rand.Range(0.3f, 0.5f);
                map.flecks.CreateFleck(dataStatic);
            }
        }
    }
}
