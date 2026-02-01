using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class HediffComp_Hurricane : HediffComp_SeverityPerDay
{
	public override void CompPostTick(ref float severityAdjustment)
	{
		if (base.Pawn.Map.weatherManager.CurWeatherPerceived != VPE_DefOf.VPE_Hurricane_Weather)
		{
			base.CompPostTick(ref severityAdjustment);
		}
	}
}
