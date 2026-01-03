using Verse;

namespace AncotLibrary;

public class HediffCompProperties_SeverityChangeByVacuum : HediffCompProperties
{
	public float standardVacuum = 0.5f;

	public float severityPerTick_High = 0.01f;

	public float severityPerTick_Low = 0.01f;

	public HediffCompProperties_SeverityChangeByVacuum()
	{
		compClass = typeof(HediffComp_SeverityChangedByVacuum);
	}
}
