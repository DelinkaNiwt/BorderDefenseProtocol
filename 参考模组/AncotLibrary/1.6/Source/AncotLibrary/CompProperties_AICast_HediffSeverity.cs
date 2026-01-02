using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AICast_HediffSeverity : CompProperties_AbilityEffect
{
	public HediffDef hediff;

	public FloatRange severityRange = new FloatRange(0f, 1f);

	public CompProperties_AICast_HediffSeverity()
	{
		compClass = typeof(CompAbilityAICast_HediffSeverity);
	}
}
