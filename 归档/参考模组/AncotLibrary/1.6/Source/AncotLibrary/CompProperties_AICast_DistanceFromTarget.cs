using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AICast_DistanceFromTarget : CompProperties_AbilityEffect
{
	public FloatRange distance = new FloatRange(0f, 5f);

	public CompProperties_AICast_DistanceFromTarget()
	{
		compClass = typeof(CompAbilityAICast_DistanceFromTarget);
	}
}
