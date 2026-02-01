using RimWorld;
using Verse;

namespace NCL;

public class CompAbilityEffect_GiveSelfHediff : CompAbilityEffect
{
	public new CompProperties_AbilityGiveSelfHediff Props => (CompProperties_AbilityGiveSelfHediff)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (parent.pawn != null && !parent.pawn.Dead)
		{
			Hediff hediff = HediffMaker.MakeHediff(Props.hediffToApply, parent.pawn);
			hediff.Severity = Props.severity;
			parent.pawn.health.AddHediff(hediff);
		}
	}
}
