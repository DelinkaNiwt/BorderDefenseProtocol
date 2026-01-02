using Verse;

namespace AncotLibrary;

public class HediffCompProperties_SeverityFollowHediff : HediffCompProperties
{
	public HediffDef hediff;

	public float defaultSeverity = 0.01f;

	public int intervalTicks = 20;

	public HediffCompProperties_SeverityFollowHediff()
	{
		compClass = typeof(HediffComp_SeverityFollowHediff);
	}
}
