using System.Collections.Generic;
using RimWorld;

namespace AncotLibrary;

public class CompProperties_AbilityOverrideByAbility : CompProperties_AbilityEffect
{
	public List<AbilityDef> abilities = new List<AbilityDef>();

	public CompProperties_AbilityOverrideByAbility()
	{
		compClass = typeof(CompAbilityOverrideByAbility);
	}
}
