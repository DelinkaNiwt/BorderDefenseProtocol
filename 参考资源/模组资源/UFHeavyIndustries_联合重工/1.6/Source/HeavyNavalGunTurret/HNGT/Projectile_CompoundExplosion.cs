using RimWorld;
using Verse;

namespace HNGT;

public class Projectile_CompoundExplosion : Projectile_Explosive
{
	protected override void Explode()
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		if (!(def.projectile is ProjectileProperties_CompoundExplosion projectileProperties_CompoundExplosion))
		{
			Log.ErrorOnce("HNGT: Projectile_CompoundExplosion (" + def.defName + ") must be used with ProjectileProperties_CompoundExplosion in XML.", def.defName.GetHashCode());
			base.Explode();
			return;
		}
		Destroy();
		Thing instigator = launcher;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		Thing thing = intendedTarget.Thing;
		float? direction = origin.AngleToFlat(destination);
		GenExplosion.DoExplosion(position, map, def.projectile.explosionRadius, base.DamageDef, instigator, DamageAmount, ArmorPenetration, def.projectile.soundExplode, weapon, projectile, thing, def.projectile.postExplosionSpawnThingDef ?? (def.projectile.explosionSpawnsSingleFilth ? null : def.projectile.filth), def.projectile.postExplosionSpawnChance, def.projectile.postExplosionSpawnThingCount, def.projectile.postExplosionGasType, null, 255, def.projectile.applyDamageToExplosionCellsNeighbors, def.projectile.preExplosionSpawnThingDef, def.projectile.preExplosionSpawnChance, def.projectile.preExplosionSpawnThingCount, def.projectile.explosionChanceToStartFire, def.projectile.explosionDamageFalloff, direction, null, null, def.projectile.doExplosionVFX, base.DamageDef.expolosionPropagationSpeed, 0f, doSoundEffects: true, def.projectile.postExplosionSpawnThingDefWater, def.projectile.screenShakeFactor, null, null, def.projectile.postExplosionSpawnSingleThingDef, def.projectile.preExplosionSpawnSingleThingDef);
		if (projectileProperties_CompoundExplosion.additionalExplosions != null && projectileProperties_CompoundExplosion.additionalExplosions.Count > 0)
		{
			foreach (ExplosionParams additionalExplosion in projectileProperties_CompoundExplosion.additionalExplosions)
			{
				if (additionalExplosion.damageDef == null || additionalExplosion.radius <= 0f)
				{
					Log.Warning("HNGT: Projectile_CompoundExplosion (" + def.defName + ") has an invalid entry in additional explosions.");
				}
				else
				{
					GenExplosion.DoExplosion(position, map, additionalExplosion.radius, additionalExplosion.damageDef, instigator, additionalExplosion.damageAmount, additionalExplosion.armorPenetration, additionalExplosion.soundExplode, weapon, projectile, thing, null, 0f, 1, null, null, 255, def.projectile.applyDamageToExplosionCellsNeighbors, null, 0f, 1, (additionalExplosion.damageDef.defName.ToLower() == "flame") ? 0.5f : 0f, def.projectile.explosionDamageFalloff, direction, null, null, doVisualEffects: false, additionalExplosion.damageDef.expolosionPropagationSpeed, 0f, doSoundEffects: true, null, def.projectile.screenShakeFactor);
				}
			}
		}
		if (def.projectile.explosionSpawnsSingleFilth && def.projectile.filth != null && def.projectile.filthCount.TrueMax > 0 && Rand.Chance(def.projectile.filthChance) && !base.Position.Filled(map))
		{
			FilthMaker.TryMakeFilth(base.Position, map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
		}
	}
}
