using RimWorld;

namespace AncotLibrary;

public class CompProperties_AbilityShiftBackward : CompProperties_AbilityEffect
{
	public float distance = 5f;

	public CompProperties_AbilityShiftBackward()
	{
		compClass = typeof(CompAbilityShiftBackward);
	}
}
