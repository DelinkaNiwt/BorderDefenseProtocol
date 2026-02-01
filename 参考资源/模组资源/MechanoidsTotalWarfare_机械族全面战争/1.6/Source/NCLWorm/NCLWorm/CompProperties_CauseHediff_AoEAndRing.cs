using Verse;

namespace NCLWorm;

public class CompProperties_CauseHediff_AoEAndRing : CompProperties
{
	public HediffDef hediff;

	public float range;

	public int checkInterval = 60;

	public bool drawLines = true;

	public CompProperties_CauseHediff_AoEAndRing()
	{
		compClass = typeof(CompCauseHediff_AoEPlus);
	}
}
