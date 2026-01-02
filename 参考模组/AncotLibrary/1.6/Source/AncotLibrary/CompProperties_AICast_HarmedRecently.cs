using RimWorld;

namespace AncotLibrary;

public class CompProperties_AICast_HarmedRecently : CompProperties_AbilityEffect
{
	public int thresholdTicks = 2500;

	public CompProperties_AICast_HarmedRecently()
	{
		compClass = typeof(CompAbilityAICast_HarmedRecently);
	}
}
