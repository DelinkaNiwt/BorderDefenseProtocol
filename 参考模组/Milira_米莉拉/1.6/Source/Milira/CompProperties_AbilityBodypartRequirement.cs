using RimWorld;

namespace Milira;

public class CompProperties_AbilityBodypartRequirement : CompProperties_AbilityEffect
{
	public string requiredBodypartdefName;

	public CompProperties_AbilityBodypartRequirement()
	{
		compClass = typeof(CompAbilityEffect_BodypartRequirement);
	}
}
