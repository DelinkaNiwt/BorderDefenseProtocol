using Verse;

namespace NCL;

public class CompProperties_ExplodeWhenStunned : CompProperties
{
	public float explosionRadius = 5f;

	public int explosionDamage = 30;

	public DamageDef damageType;

	public float armorPenetration = 1f;

	public float chanceToStartFire = 0.5f;

	public HediffDef vulnerabilityHediff;

	public float increasedDamageFactor = 1.5f;

	public bool damageFalloff = true;

	public bool applyDamageToExplosionCellsNeighbors = true;

	public float cooldownTicks = 600f;

	public CompProperties_ExplodeWhenStunned()
	{
		compClass = typeof(CompExplodeWhenStunned);
	}
}
