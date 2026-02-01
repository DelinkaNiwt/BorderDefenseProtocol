using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class ProjectileAntiAir : Bullet
    {
        private int cachedTicksToImpact;

        private bool missedTarget;

        private bool launched;

        public bool IsExplosive => def.projectile.damageDef.isExplosive;

        public bool Guided
        {
            get
            {
                IsAntiAirProj ext = def.GetModExtension<IsAntiAirProj>();
                if (ext != null)
                {
                    return ext.guided;
                }
                return false;
            }
        }

        public float SpeedTilesPerTick
        {
            get
            {
                IsAntiAirProj ext = def.GetModExtension<IsAntiAirProj>();
                if (ext != null && ext.accelerate && !launched && cachedTicksToImpact > 30)
                {
                    int age = cachedTicksToImpact - lifetime;
                    return (Math.Min(age, 30) / 30f) * def.projectile.SpeedTilesPerTick;
                }
                return def.projectile.SpeedTilesPerTick;
            }
        }

        public override Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (destination - origin).Yto0().normalized * SpeedTilesPerTick * (cachedTicksToImpact - lifetime) * 0.95f;
                return origin.Yto0() + b + Vector3.up * def.Altitude;
            }
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            destination = intendedTarget.Cell.ToVector3Shifted() * 0.7f + destination * 0.3f;
            cachedTicksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
        }

        protected override void TickInterval(int delta)
        {
            lifetime -= delta;
            if (landed)
            {
                return;
            }

            int age = cachedTicksToImpact - lifetime;
            if (!missedTarget && Guided && age < 120)
            {
                if (!intendedTarget.HasThing || intendedTarget.ThingDestroyed)
                {
                    if (launcher != null && launcher is Building_TurretGun building)
                    {
                        LocalTargetInfo newTarget = Building_AntiAirTurret.TryFindNewTargetStatic(building, building.AttackVerb);
                        if (newTarget != null && newTarget.HasThing)
                        {
                            Launch(launcher, DrawPos, newTarget, newTarget, ProjectileHitFlags.IntendedTarget, false, equipment);
                            launched = true;
                            return;
                        }
                    }
                }
                else
                {
                    destination = intendedTarget.CenterVector3;
                }
            }

            Vector3 exactPosition = ExactPosition;
            ticksToImpact -= delta;
            if (!ExactPosition.InBounds(base.Map))
            {
                ticksToImpact += delta;
                //base.Position = ExactPosition.ToIntVec3();
                Destroy();
                return;
            }

            if (IsExplosive)
            {
                for (int i = 0; i < 4; i++)
                {
                    ThrowFleck(DrawPos, Map, FleckDefOf.Smoke, 0.4f, (destination - origin).Yto0().AngleFlat() + 180f);
                }
            }

            Vector3 exactPosition2 = ExactPosition;
            if ((bool)typeof(Projectile).GetMethod("CheckForFreeInterceptBetween", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this, new object[] { exactPosition, exactPosition2 }))
            {
                return;
            }

            base.Position = ExactPosition.ToIntVec3();
            if (ticksToImpact <= 0)
            {
                //if (DestinationCell.InBounds(base.Map))
                //{
                //    base.Position = DestinationCell;
                //}
                ImpactSomething();
            }
        }

        protected override void ImpactSomething()
        {
            if (intendedTarget.HasThing && !missedTarget && CanHitNew(intendedTarget.Thing))
            {
                Pawn pawn = intendedTarget.Thing as Pawn;
                float distance = (origin - destination).MagnitudeHorizontal();
                //命中概率：实弹远远小于导弹
                //目标为pawn时精度低，目标为弹头时精度为50%
                float num = Guided ? 384f : 4f;
                bool flag = pawn != null && Rand.Chance(num / (distance + num));
                if (flag)
                {
                    Impact(intendedTarget.Thing);
                    return;
                }
                Projectile proj = intendedTarget.Thing as Projectile;
                bool flag2 = proj != null && (Guided || Rand.Chance(0.5f));
                if (flag2)
                {
                    Impact(intendedTarget.Thing);
                    return;
                }
                missedTarget = true;
            }

            IEnumerable<IntVec3> vecs = GenRadial.RadialCellsAround(base.Position, IsExplosive ? 1.9f : 1.0f, true);
            List<Thing> list = new List<Thing>();
            foreach (IntVec3 c in vecs)
            {
                if (!c.InBounds(base.Map))
                {
                    continue;
                }
                list.AddRange(VerbUtility.ThingsToHit(c, base.Map, CanHitNew));
            }
            list.Shuffle();
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                Pawn pawn2 = thing as Pawn;
                float num;
                if (pawn2 != null)
                {
                    num = 1f * Mathf.Clamp(pawn2.BodySize, 0.1f, 2f);
                    //if (pawn2.GetPosture() != 0 && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f)
                    //{
                    //    num *= 0.5f;
                    //}

                    if (launcher != null && pawn2.Faction != null && launcher.Faction != null && !pawn2.Faction.HostileTo(launcher.Faction))
                    {
                        num *= VerbUtility.InterceptChanceFactorFromDistance(origin, base.Position);
                    }
                }
                else
                {
                    num = 1.5f * thing.def.fillPercent;
                }

                if (Rand.Chance(num))
                {
                    Impact(list.RandomElement());
                    return;
                }
            }
        }

        private bool CanHitNew(Thing thing)
        {
            if (!thing.Spawned)
            {
                return false;
            }

            if (thing == launcher)
            {
                return false;
            }

            ProjectileHitFlags hitFlags = HitFlags;
            if (hitFlags == ProjectileHitFlags.None)
            {
                return false;
            }

            if (thing.Map != base.Map)
            {
                return false;
            }

            if (CoverUtility.ThingCovered(thing, base.Map))
            {
                return false;
            }

            if (thing == intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != 0)
            {
                return true;
            }

            if (thing != intendedTarget)
            {
                if (thing is Pawn p && p.Flying)
                {
                    return true;
                }
            }

            if (thing == intendedTarget && thing.def.Fillage == FillCategory.Full)
            {
                return true;
            }

            return false;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            bool flag = true;
            if (IsExplosive)
            {
                Explode();
                landed = true;
                flag = false;
                GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, base.DamageDef, launcher.Faction, launcher);
                if (!Destroyed) this.Destroy();
            }
            if (hitThing != null && hitThing is Projectile proj && proj.def.projectile.flyOverhead)
            {
                //proj.Launch(null, proj.PositionHeld, proj.PositionHeld, ProjectileHitFlags.All);
                object[] para = { null, false };
                typeof(Projectile).GetMethod("Impact", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hitThing, para);
                if (!Destroyed) this.Destroy();
                flag = false;
            }
            if (flag) base.Impact(hitThing, blockedByShield);
        }

        protected virtual void Explode()
        {
            Map map = base.Map;
            //Destroy();
            if (def.projectile.explosionEffect != null)
            {
                Effecter effecter = def.projectile.explosionEffect.Spawn();
                if (def.projectile.explosionEffectLifetimeTicks != 0)
                {
                    map.effecterMaintainer.AddEffecterToMaintain(effecter, base.Position.ToVector3().ToIntVec3(), def.projectile.explosionEffectLifetimeTicks);
                }
                else
                {
                    effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
                    effecter.Cleanup();
                }
            }
            IntVec3 position = intendedTarget.HasThing ? intendedTarget.Cell : base.Position;
            float explosionRadius = def.projectile.explosionRadius;
            DamageDef damageDef = base.DamageDef;
            Thing instigator = launcher;
            int damageAmount = DamageAmount;
            float armorPenetration = ArmorPenetration;
            SoundDef soundExplode = def.projectile.soundExplode;
            ThingDef weapon = equipmentDef;
            ThingDef projectile = def;
            Thing thing = intendedTarget.Thing;
            ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef ?? (def.projectile.explosionSpawnsSingleFilth ? null : def.projectile.filth);
            ThingDef postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
            float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
            GasType? postExplosionGasType = def.projectile.postExplosionGasType;
            ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
            float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
            int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
            bool applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
            float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
            bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
            float? direction = origin.AngleToFlat(destination);
            float expolosionPropagationSpeed = base.DamageDef.expolosionPropagationSpeed;
            float screenShakeFactor = def.projectile.screenShakeFactor;
            bool doExplosionVFX = def.projectile.doExplosionVFX;
            ThingDef preExplosionSpawnSingleThingDef = def.projectile.preExplosionSpawnSingleThingDef;
            ThingDef postExplosionSpawnSingleThingDef = def.projectile.postExplosionSpawnSingleThingDef;
            GenExplosion.DoExplosion(position, map, explosionRadius, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, null, doExplosionVFX, expolosionPropagationSpeed, 0f, doSoundEffects: true, postExplosionSpawnThingDefWater, screenShakeFactor, null, null, postExplosionSpawnSingleThingDef, preExplosionSpawnSingleThingDef);
            if (def.projectile.explosionSpawnsSingleFilth && def.projectile.filth != null && def.projectile.filthCount.TrueMax > 0 && Rand.Chance(def.projectile.filthChance) && !base.Position.Filled(map))
            {
                FilthMaker.TryMakeFilth(base.Position, map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
            }
        }

        public void ThrowFleck(Vector3 loc, Map map, FleckDef def, float size, float angle)
        {
            if (loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, def, Rand.Range(1.5f, 2.5f) * size);
                dataStatic.rotationRate = Rand.Range(-30f, 30f);
                dataStatic.velocityAngle = angle;
                dataStatic.velocitySpeed = Rand.Range(0.4f, 0.8f);
                dataStatic.solidTimeOverride = 0.5f;
                map.flecks.CreateFleck(dataStatic);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cachedTicksToImpact, "cachedTicksToImpact");
            Scribe_Values.Look(ref missedTarget, "missedTarget");
            Scribe_Values.Look(ref launched, "launched");
        }
    }
}
