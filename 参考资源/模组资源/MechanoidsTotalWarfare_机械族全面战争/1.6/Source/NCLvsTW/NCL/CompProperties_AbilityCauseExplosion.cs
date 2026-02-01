using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_AbilityCauseExplosion : CompProperties_AbilityEffect
{
	public DamageDef damageDef;

	public int damageAmount;

	public float explosionRadius;

	public float chanceToStartFire = 0f;

	public bool damageFalloff = false;

	public ThingDef preExplosionSpawnThingDef = null;

	public float preExplosionSpawnChance = 0f;

	public int preExplosionSpawnThingCount = 1;

	public CompProperties_AbilityCauseExplosion()
	{
		compClass = typeof(CompAbilityEffect_Explosion);
	}
}
