using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityProjectShield : CompProperties_AbilityEffect
{
	public ThingDef mechShieldType = ThingDefOf.MechShield;

	public int hitPointBase = 100;

	public CompProperties_AbilityProjectShield()
	{
		compClass = typeof(CompAbilityProjectShield);
	}
}
