using System.Collections.Generic;
using RimWorld;

namespace AncotLibrary;

public class CompProperties_AbilityOnlyRace : CompProperties_AbilityEffect
{
	public List<string> races;

	public CompProperties_AbilityOnlyRace()
	{
		compClass = typeof(CompAbilityOnlyRace);
	}
}
