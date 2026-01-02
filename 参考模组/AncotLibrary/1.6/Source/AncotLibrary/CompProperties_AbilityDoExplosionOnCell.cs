using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityDoExplosionOnCell : CompProperties_AbilityEffect
{
	public float radius = 2f;

	public int damageAmount = 50;

	public float armorPenetration = 1f;

	public DamageDef damageDef = DamageDefOf.Bomb;

	public bool ignoreCasterCell = true;

	public EffecterDef warmupEffect;

	public int warmupEffectMaintainTicks = 17;

	public bool targetOnCaster = false;

	public CompProperties_AbilityDoExplosionOnCell()
	{
		compClass = typeof(CompAbilityDoExplosionOnCell);
	}
}
