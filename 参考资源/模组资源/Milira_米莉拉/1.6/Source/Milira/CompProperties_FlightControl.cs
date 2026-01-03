using Verse;

namespace Milira;

public class CompProperties_FlightControl : CompProperties
{
	public BodyPartDef bodyPart;

	public float hungerPctThresholdCanFly = 0.1f;

	public float hungerPctCostPerSecondFly = 0.01f;

	public CompProperties_FlightControl()
	{
		compClass = typeof(CompFlightControl);
	}
}
