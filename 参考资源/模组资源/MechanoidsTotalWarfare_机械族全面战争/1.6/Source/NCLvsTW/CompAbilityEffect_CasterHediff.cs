using System.Collections.Generic;
using NCL;
using RimWorld;
using Verse;

public class CompAbilityEffect_CasterHediff : CompAbilityEffect
{
	public new CompProperties_AbilityCasterHediff Props => props as CompProperties_AbilityCasterHediff;

	public override void PostApplied(List<LocalTargetInfo> targets, Map map)
	{
		Hediff firstHediffOfDef = parent.pawn.health.hediffSet.GetFirstHediffOfDef(Props.casterHediff);
		if (!Props.ignoreIfExist || firstHediffOfDef == null)
		{
			Hediff hediff = parent.pawn.health.AddHediff(Props.casterHediff);
			hediff.Severity = Props.initialSeverity;
		}
	}
}
