using RimWorld;

namespace NCL;

public class CompProperties_AbilityShowRange : CompProperties_AbilityEffect
{
	public float range = 0f;

	public float minRange = 0f;

	public CompProperties_AbilityShowRange()
	{
		compClass = typeof(CompAbilityShowRange);
	}
}
