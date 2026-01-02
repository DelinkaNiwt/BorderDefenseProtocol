using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityEffect_GiveHediffOnSelf : CompAbilityEffect
{
	private Pawn Pawn => parent.pawn;

	private new CompProperties_AbilityGiveHediffOnSelf Props => (CompProperties_AbilityGiveHediffOnSelf)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (Props.replaceExisting)
		{
			Hediff firstHediffOfDef = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
			if (firstHediffOfDef != null)
			{
				Pawn.health.RemoveHediff(firstHediffOfDef);
			}
		}
		Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, Pawn, Props.onlyBrain ? Pawn.health.hediffSet.GetBrain() : null);
		if (Props.severity >= 0f)
		{
			hediff.Severity = Props.severity;
		}
		Pawn.health.AddHediff(hediff);
		base.Apply(target, dest);
	}
}
