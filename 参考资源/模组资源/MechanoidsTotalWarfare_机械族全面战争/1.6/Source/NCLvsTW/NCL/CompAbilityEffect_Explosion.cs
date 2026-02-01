using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class CompAbilityEffect_Explosion : CompAbilityEffect
{
	public new CompProperties_AbilityCauseExplosion Props => (CompProperties_AbilityCauseExplosion)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		IntVec3 cell = target.Cell;
		Map map = parent.ConstantCaster.Map;
		float explosionRadius = Props.explosionRadius;
		DamageDef damageDef = Props.damageDef;
		Thing constantCaster = parent.ConstantCaster;
		int damageAmount = Props.damageAmount;
		ThingDef preExplosionSpawnThingDef = Props.preExplosionSpawnThingDef;
		float preExplosionSpawnChance = Props.preExplosionSpawnChance;
		int preExplosionSpawnThingCount = Props.preExplosionSpawnThingCount;
		float chanceToStartFire = Props.chanceToStartFire;
		bool damageFalloff = Props.damageFalloff;
		float? direction = null;
		List<Thing> ignoredThings = new List<Thing> { parent.ConstantCaster };
		GenExplosion.DoExplosion(cell, map, explosionRadius, damageDef, constantCaster, damageAmount, -1f, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, chanceToStartFire, damageFalloff, direction, ignoredThings);
	}
}
