using RimWorld;

namespace AncotLibrary;

public class CompProperties_AbilityCheckClearArea : CompProperties_AbilityEffect
{
	public float radius = 1f;

	public bool canRoofed = false;

	public CompProperties_AbilityCheckClearArea()
	{
		compClass = typeof(CompAbilityCheckArea);
	}
}
