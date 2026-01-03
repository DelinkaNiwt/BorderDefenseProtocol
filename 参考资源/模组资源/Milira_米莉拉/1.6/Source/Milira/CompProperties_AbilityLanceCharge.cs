using RimWorld;

namespace Milira;

public class CompProperties_AbilityLanceCharge : CompProperties_AbilityEffect
{
	public float range;

	public float lineWidthEnd;

	public CompProperties_AbilityLanceCharge()
	{
		compClass = typeof(CompAbilityEffect_LanceCharge);
	}
}
