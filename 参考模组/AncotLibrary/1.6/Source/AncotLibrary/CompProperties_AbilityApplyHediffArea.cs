using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityApplyHediffArea : CompProperties_AbilityEffect
{
	public float radius = 2f;

	public float severity = 1f;

	public HediffDef hediff;

	public bool applyOnAlly = true;

	public bool applyOnAllyOnly = false;

	public bool applyOnMech = true;

	public bool ignoreCaster = false;

	public bool targetOnCaster = false;

	public EffecterDef effecter;

	public ThingDef moteAttachedToPawn;

	public CompProperties_AbilityApplyHediffArea()
	{
		compClass = typeof(CompAbilityApplyHediffArea);
	}
}
