using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityPlaceBuildingInFront : CompProperties_AbilityEffect
{
	public ThingDef building;

	public bool setFaction = true;

	public EffecterDef effecter;

	public CompProperties_AbilityPlaceBuildingInFront()
	{
		compClass = typeof(CompAbilityPlaceBuildingInFront);
	}
}
