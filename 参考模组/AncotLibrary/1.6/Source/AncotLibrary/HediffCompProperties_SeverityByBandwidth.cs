using Verse;

namespace AncotLibrary;

public class HediffCompProperties_SeverityByBandwidth : HediffCompProperties
{
	public SimpleCurve curve;

	public float severityDefault = 0.1f;

	public bool ignoreSelfBandwidthCost = false;

	public int checkTick = 10;

	public HediffCompProperties_SeverityByBandwidth()
	{
		compClass = typeof(HediffComp_SeverityByBandwidth);
	}
}
