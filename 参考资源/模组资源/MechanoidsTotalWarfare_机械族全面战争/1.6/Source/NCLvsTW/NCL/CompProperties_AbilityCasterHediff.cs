using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_AbilityCasterHediff : CompProperties_AbilityEffect
{
	public HediffDef casterHediff;

	public float initialSeverity = 0.5f;

	public bool ignoreIfExist;

	public CompProperties_AbilityCasterHediff()
	{
		compClass = typeof(CompAbilityEffect_CasterHediff);
	}
}
