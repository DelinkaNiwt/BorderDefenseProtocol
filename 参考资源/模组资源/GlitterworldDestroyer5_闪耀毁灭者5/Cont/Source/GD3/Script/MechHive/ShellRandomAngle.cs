using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class ShellRandomAngle : Skyfaller
    {
        public override void PostMake()
        {
            base.PostMake();
            if (def.skyfaller.MakesShrapnel)
            {
                shrapnelDirection = Rand.Range(95f, 135f);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad)
            {
                return;
            }
            angle = GenMath.PositiveMod(shrapnelDirection, 360f) - 90;
        }

        protected override void Impact()
        {
            hasImpacted = true;
            if (def.skyfaller.CausesExplosion)
            {
                IntVec3 position = base.Position;
                Map map = base.Map;
                float explosionRadius = def.skyfaller.explosionRadius;
                DamageDef explosionDamage = def.skyfaller.explosionDamage;
                int damAmount = GenMath.RoundRandom((float)def.skyfaller.explosionDamage.defaultDamage * def.skyfaller.explosionDamageFactor);
                List<Thing> ignoredThings = (!def.skyfaller.damageSpawnedThings) ? innerContainer.ToList() : null;
                ThingDef filthToSpawn = explosionDamage == DamageDefOf.Flame ? ThingDefOf.Filth_Fuel : null;
                float spawnChance = explosionDamage == DamageDefOf.Flame ? 0.6f : 0f;
                float fireChance = explosionDamage == DamageDefOf.Flame ? 0.6f : 0.1f;
                ThingDef singleThingDef = position.DistanceToEdge(map) < explosionRadius ? null : ThingDefOf.CraterMedium;
                if (def.skyfaller.explosionDamageFactor > 10)
                {
                    Effecter effecter = GDDefOf.GiantExplosion.Spawn().Trigger(new TargetInfo(PositionHeld, map), new TargetInfo(PositionHeld, map));
                    effecter.Cleanup();
                }
                GenExplosion.DoExplosion(position, map, explosionRadius, explosionDamage, null, damAmount, 1.45f, null, null, null, null, filthToSpawn, spawnChance, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0, 1, fireChance, damageFalloff: true, null, ignoredThings, null, true, 1, 0, true, null, 1, null, null, singleThingDef);
            }

            SpawnThings();
            innerContainer.ClearAndDestroyContents();
            CellRect cellRect = this.OccupiedRect();
            for (int i = 0; i < cellRect.Area * def.skyfaller.motesPerCell; i++)
            {
                FleckMaker.ThrowDustPuff(cellRect.RandomVector3, base.Map, 2f);
            }

            if (def.skyfaller.MakesShrapnel)
            {
                SkyfallerShrapnelUtility.MakeShrapnel(base.Position, base.Map, shrapnelDirection, def.skyfaller.shrapnelDistanceFactor, def.skyfaller.metalShrapnelCountRange.RandomInRange, def.skyfaller.rubbleShrapnelCountRange.RandomInRange, spawnMotes: true);
            }

            if (def.skyfaller.cameraShake > 0f && base.Map == Find.CurrentMap)
            {
                Find.CameraDriver.shaker.DoShake(def.skyfaller.cameraShake);
            }

            if (def.skyfaller.impactSound != null)
            {
                def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map)));
            }

            if (impactLetter != null)
            {
                Find.LetterStack.ReceiveLetter(impactLetter);
            }

            Map map2 = base.Map;
            Destroy();
            if (def.skyfaller.spawnThing != null)
            {
                GenSpawn.TrySpawn(def.skyfaller.spawnThing, base.Position, map2, out Thing _);
            }
        }
    }
}
