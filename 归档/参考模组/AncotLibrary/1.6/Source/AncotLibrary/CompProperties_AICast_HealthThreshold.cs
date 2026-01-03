using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AICast_HealthThreshold : CompProperties_AbilityEffect
{
	public FloatRange healthPctRange = new FloatRange(0f, 1f);

	public CompProperties_AICast_HealthThreshold()
	{
		compClass = typeof(CompAbilityAICast_HealthThreshold);
	}
}
