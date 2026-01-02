using Verse;

namespace AncotLibrary;

public class CompProperties_GetHediff_ApplyDamage : CompProperties
{
	public HediffDef hediffDef;

	public float severityPerHit = 1f;

	public BodyPartDef bodyPartDef;

	public CompProperties_GetHediff_ApplyDamage()
	{
		compClass = typeof(CompGetHediff_ApplyDamage);
	}
}
