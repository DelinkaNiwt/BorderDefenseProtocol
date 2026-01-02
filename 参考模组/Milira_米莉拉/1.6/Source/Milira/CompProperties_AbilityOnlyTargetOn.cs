using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_AbilityOnlyTargetOn : CompProperties_AbilityEffect
{
	public List<ThingDef> thingDefs;

	public List<PawnKindDef> pawnkindDefs;

	public CompProperties_AbilityOnlyTargetOn()
	{
		compClass = typeof(CompAbilityEffect_OnlyTargetOn);
	}
}
