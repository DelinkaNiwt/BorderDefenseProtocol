using RimWorld;
using Verse;

namespace NCL;

public class LTS_CompProperties_ToggleHediff : CompProperties_AbilityEffect
{
	public HediffDef ToggleHediff;

	public float StartSeverity;

	public BodyPartDef location = null;

	public LTS_CompProperties_ToggleHediff()
	{
		compClass = typeof(LTS_CompAbilityEffect_ToggleHediff);
	}
}
