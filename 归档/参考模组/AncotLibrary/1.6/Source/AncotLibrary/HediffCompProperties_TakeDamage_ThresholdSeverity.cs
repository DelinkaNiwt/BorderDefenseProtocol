using Verse;

namespace AncotLibrary;

public class HediffCompProperties_TakeDamage_ThresholdSeverity : HediffCompProperties
{
	public float thresholdSeverity = 1f;

	public float damageAmountBase = 10f;

	public float armorPenetrationBase = 0.1f;

	public float stoppingPower = 0.5f;

	public float decreaseSeverityTriggered = 1f;

	public DamageDef damageDef;

	public HediffCompProperties_TakeDamage_ThresholdSeverity()
	{
		compClass = typeof(HediffComp_TakeDamage_ThresholdSeverity);
	}
}
