using RimWorld;

namespace AncotLibrary;

public class CompProperties_AICast_FireNearby : CompProperties_AbilityEffect
{
	public float radius = 2f;

	public CompProperties_AICast_FireNearby()
	{
		compClass = typeof(CompAbilityAICast_FireNearby);
	}
}
