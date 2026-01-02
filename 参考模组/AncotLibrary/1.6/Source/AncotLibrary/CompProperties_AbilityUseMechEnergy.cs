using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityUseMechEnergy : CompProperties_AbilityEffect
{
	public float energyPerUse = 0.1f;

	public bool canUseIfNoEnergyNeed = true;

	public CompProperties_AbilityUseMechEnergy()
	{
		compClass = typeof(CompAbilityUseMechEnergy);
	}

	public override IEnumerable<string> ExtraStatSummary()
	{
		yield return "Ancot.AbilityMechEnergyCost".Translate(energyPerUse.ToStringPercent());
	}
}
