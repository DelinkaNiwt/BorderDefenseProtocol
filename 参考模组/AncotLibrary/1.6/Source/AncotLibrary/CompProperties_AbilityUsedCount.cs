using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityUsedCount : CompProperties_AbilityEffect
{
	public int totalNum;

	public HediffDef removeHediff;

	public CompProperties_AbilityUsedCount()
	{
		compClass = typeof(CompAbilityUsedCount);
	}
}
