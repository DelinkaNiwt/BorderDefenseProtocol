using Verse;

namespace AncotLibrary;

public class HediffCompProperties_ApplyDamage_SeverityChange : HediffCompProperties
{
	public float severityChange = 0.1f;

	public float minSeverity = 0.01f;

	public float maxSeverity = 1f;

	public bool explosiveOnly = false;

	public bool removeHediff = false;

	public HediffCompProperties_ApplyDamage_SeverityChange()
	{
		compClass = typeof(HediffCompApplyDamage_SeverityChange);
	}
}
