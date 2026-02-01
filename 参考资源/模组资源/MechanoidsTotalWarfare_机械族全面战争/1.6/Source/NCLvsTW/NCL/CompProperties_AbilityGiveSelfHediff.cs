using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_AbilityGiveSelfHediff : CompProperties_AbilityEffect
{
	public HediffDef hediffToApply;

	public float severity = 1f;

	public CompProperties_AbilityGiveSelfHediff()
	{
		compClass = typeof(CompAbilityEffect_GiveSelfHediff);
	}
}
