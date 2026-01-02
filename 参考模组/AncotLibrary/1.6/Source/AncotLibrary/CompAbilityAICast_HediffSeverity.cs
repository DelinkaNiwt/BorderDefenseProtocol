using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_HediffSeverity : CompAbilityEffect
{
	private new CompProperties_AICast_HediffSeverity Props => (CompProperties_AICast_HediffSeverity)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Caster.Spawned && !Caster.Downed)
		{
			Hediff firstHediffOfDef = Caster.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
			if (firstHediffOfDef != null)
			{
				return Props.severityRange.Includes(firstHediffOfDef.Severity);
			}
			return false;
		}
		return false;
	}
}
