using Verse;

namespace AncotLibrary;

public class HediffComp_SeverityFollowHediff : HediffComp
{
	private HediffCompProperties_SeverityFollowHediff Props => (HediffCompProperties_SeverityFollowHediff)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(Props.intervalTicks, delta))
		{
			Hediff firstHediffOfDef = base.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
			if (firstHediffOfDef != null)
			{
				parent.Severity = base.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff).Severity;
			}
			else
			{
				parent.Severity = Props.defaultSeverity;
			}
		}
	}
}
