using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffCompProperties_ApplyDamage_ExplodeChance : HediffCompProperties
{
	public float chance = 0.1f;

	public int damageAmountBase = 10;

	public float armorPenetrationBase = 0f;

	public DamageDef damageDef = DamageDefOf.Bomb;

	public float explosionRadius = 3f;

	public float startTriggerHealthPct = 1f;

	public bool dieInExplosion = false;

	public FloatRange triggerAngle;

	public int cooldownTicks = -1;

	public AbilityDef cooldownAbility;

	public HediffCompProperties_ApplyDamage_ExplodeChance()
	{
		compClass = typeof(HediffCompApplyDamage_ExplodeChance);
	}
}
