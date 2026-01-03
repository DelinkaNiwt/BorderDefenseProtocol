using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_AbilitySpear : CompProperties_AbilityEffect
{
	public ThingDef projectileDef;

	public EffecterDef sprayEffecter;

	public float radius;

	public CompProperties_AbilitySpear()
	{
		compClass = typeof(CompAbilityEffect_Spear);
	}
}
