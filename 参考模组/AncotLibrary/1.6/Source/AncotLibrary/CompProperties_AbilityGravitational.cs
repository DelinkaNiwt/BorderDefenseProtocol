using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityGravitational : CompProperties_AbilityEffect
{
	public int distance = 10;

	public List<HediffDef> removeHediffsAffected;

	public CompProperties_AbilityGravitational()
	{
		compClass = typeof(CompAbilityGravitational);
	}
}
