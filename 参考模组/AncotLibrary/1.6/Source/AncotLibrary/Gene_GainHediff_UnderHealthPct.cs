using Verse;

namespace AncotLibrary;

public class Gene_GainHediff_UnderHealthPct : Gene
{
	private int cooldownTicksLeft = 0;

	public ModExtension_GeneHediff Props => def.GetModExtension<ModExtension_GeneHediff>();

	public override void TickInterval(int delta)
	{
		base.TickInterval(delta);
		cooldownTicksLeft -= delta;
		if (cooldownTicksLeft < 0 && pawn.health.summaryHealth.SummaryHealthPercent < Props.healthPctThreshold)
		{
			HealthUtility.AdjustSeverity(pawn, Props.hediff, Props.severity);
			cooldownTicksLeft = Props.cooldownTicks;
		}
	}

	public override void PostRemove()
	{
		base.PostRemove();
		Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
		firstHediffOfDef.Severity = 0f;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref cooldownTicksLeft, "cooldownTicksLeft", 0);
	}
}
