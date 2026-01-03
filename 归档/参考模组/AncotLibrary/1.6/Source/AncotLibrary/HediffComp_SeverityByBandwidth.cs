using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_SeverityByBandwidth : HediffComp
{
	private HediffCompProperties_SeverityByBandwidth Props => (HediffCompProperties_SeverityByBandwidth)props;

	private Pawn Mechanitor => base.Pawn.GetOverseer();

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(Props.checkTick, delta))
		{
			if (Mechanitor != null)
			{
				float x = (Props.ignoreSelfBandwidthCost ? Mechanitor.GetStatValue(StatDefOf.MechBandwidth) : (Mechanitor.GetStatValue(StatDefOf.MechBandwidth) - base.Pawn.GetStatValue(StatDefOf.BandwidthCost)));
				parent.Severity = Props.curve.Evaluate(x);
			}
			else
			{
				parent.Severity = Props.severityDefault;
			}
		}
	}
}
