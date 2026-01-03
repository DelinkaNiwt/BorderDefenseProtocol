using Verse;

namespace AncotLibrary;

public class HediffCompProperties_TakeDamage_DOT : HediffCompProperties
{
	public float damageAmountBase = 10f;

	public float armorPenetrationBase = 0.1f;

	public float stoppingPower = 0.5f;

	public float severityPerTime = -0.1f;

	public int ticksBetweenDamage = 60;

	public DamageDef damageDef;

	public HediffCompProperties_TakeDamage_DOT()
	{
		compClass = typeof(HediffComp_TakeDamage_DOT);
	}
}
