using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_AbilityBow : CompProperties_AbilityEffect
{
	public ThingDef skyfallerDef;

	public EffecterDef sprayEffecter;

	public float amount;

	public float radius;

	public CompProperties_AbilityBow()
	{
		compClass = typeof(CompAbilityEffect_Bow);
	}
}
