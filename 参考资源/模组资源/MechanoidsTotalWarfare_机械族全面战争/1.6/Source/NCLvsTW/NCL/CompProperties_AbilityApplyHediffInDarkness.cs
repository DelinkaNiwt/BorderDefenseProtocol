using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_AbilityApplyHediffInDarkness : CompProperties_AbilityEffect
{
	public HediffDef hediffToApply;

	public float maxRange = 250f;

	public float darknessThreshold = 0.3f;

	public CompProperties_AbilityApplyHediffInDarkness()
	{
		compClass = typeof(CompAbilityEffect_ApplyHediffInDarkness);
	}
}
