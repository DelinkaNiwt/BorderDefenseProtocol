using RimWorld;
using Verse;

namespace Milira;

public class HediffComp_Flight : HediffComp
{
	private HediffCompProperties_Flight Props => (HediffCompProperties_Flight)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		Pawn_FlightTracker flight = base.Pawn.flight;
		if (flight != null && !flight.Flying)
		{
			parent.Severity -= parent.Severity;
		}
	}
}
