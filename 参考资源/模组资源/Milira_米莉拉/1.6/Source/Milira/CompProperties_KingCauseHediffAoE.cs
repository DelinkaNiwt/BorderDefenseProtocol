using Verse;

namespace Milira;

public class CompProperties_KingCauseHediffAoE : CompProperties
{
	public HediffDef hediff;

	public int timeInterval = 240;

	public CompProperties_KingCauseHediffAoE()
	{
		compClass = typeof(CompKingCauseHediffAoE);
	}
}
