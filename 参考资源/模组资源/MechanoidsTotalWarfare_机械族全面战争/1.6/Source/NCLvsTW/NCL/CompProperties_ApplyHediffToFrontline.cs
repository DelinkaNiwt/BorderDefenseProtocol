using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_ApplyHediffToFrontline : CompProperties_AbilityEffect
{
	public HediffDef hediffToApply;

	public int numberOfTargets = 5;

	public bool affectEnemies = false;

	public bool includeDowned = false;

	public CompProperties_ApplyHediffToFrontline()
	{
		compClass = typeof(CompAbilityEffect_ApplyHediffToFrontline);
	}
}
