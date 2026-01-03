using RimWorld;

namespace AncotLibrary;

public class CompProperties_AbilityShowRange : CompProperties_AbilityEffect
{
	public float range = 0f;

	public float minRange = 0f;

	public CompProperties_AbilityShowRange()
	{
		compClass = typeof(CompAbilityShowRange);
	}
}
