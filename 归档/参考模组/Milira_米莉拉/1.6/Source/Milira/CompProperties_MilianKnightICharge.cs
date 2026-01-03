using Verse;

namespace Milira;

public class CompProperties_MilianKnightICharge : CompProperties
{
	public HediffDef hediffDef;

	public float minSpeed = 1f;

	public float? speedSeverityFactor;

	public float initialSeverity = 0.01f;

	public float severityPerTick_Job = 0.01f;

	public float severityPerTick_Stop = 0.1f;

	public float pathCostThreshold = 40f;

	public float blockedSeverityFactor = 10f;

	public float staggeredSeverityFactor = 1f;

	public BodyPartDef bodyPartDef;

	public CompProperties_MilianKnightICharge()
	{
		compClass = typeof(CompMilianKnightICharge);
	}
}
