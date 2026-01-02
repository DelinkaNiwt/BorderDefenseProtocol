using RimWorld;

namespace Milira;

public class CompProperties_AbilitySickle : CompProperties_AbilityEffect
{
	public float radius = 4f;

	public CompProperties_AbilitySickle()
	{
		compClass = typeof(CompAbilityEffect_Sickle);
	}
}
