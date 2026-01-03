using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_HealthThreshold : CompAbilityEffect
{
	private new CompProperties_AICast_HealthThreshold Props => (CompProperties_AICast_HealthThreshold)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Props.healthPctRange.Includes(Caster.health.summaryHealth.SummaryHealthPercent))
		{
			return true;
		}
		return false;
	}
}
