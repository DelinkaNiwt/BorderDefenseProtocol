using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityGiveHediffOnSelf : CompProperties_AbilityEffect
{
	public HediffDef hediffDef;

	public bool replaceExisting = true;

	public bool onlyBrain;

	public float severity = 1f;

	public CompProperties_AbilityGiveHediffOnSelf()
	{
		compClass = typeof(CompAbilityEffect_GiveHediffOnSelf);
	}
}
