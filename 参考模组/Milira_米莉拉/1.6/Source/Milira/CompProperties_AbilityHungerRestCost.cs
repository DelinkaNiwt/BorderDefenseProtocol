using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_AbilityHungerRestCost : CompProperties_AbilityEffect
{
	public float hungerCost;

	public float restCost;

	public float hungerThreshold = 0.2f;

	public float restThreshold = 0.3f;

	public CompProperties_AbilityHungerRestCost()
	{
		compClass = typeof(CompAbilityEffect_HungerRestCost);
	}

	public override IEnumerable<string> ExtraStatSummary()
	{
		yield return "Milira.AbilityHungerCost".Translate() + ": " + hungerCost.ToStringPercent();
		yield return "Milira.AbilityRestCost".Translate() + ": " + restCost.ToStringPercent();
	}
}
