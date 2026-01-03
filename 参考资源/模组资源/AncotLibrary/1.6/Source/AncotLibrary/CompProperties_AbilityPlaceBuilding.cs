using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityPlaceBuilding : CompProperties_AbilityEffect
{
	public ThingDef building;

	public bool setFaction = true;

	public FleckDef fleckOnPlace = FleckDefOf.BroadshieldActivation;

	public SoundDef soundOnPlace = SoundDefOf.Broadshield_Startup;

	public CompProperties_AbilityPlaceBuilding()
	{
		compClass = typeof(CompAbilityPlaceBuilding);
	}
}
