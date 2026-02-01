using Verse;

namespace NCL;

public class HediffCompProperties_SolarDisruption : HediffCompProperties
{
	public float sunlightThreshold = 0.5f;

	public HediffDef solarDisruptionHediff;

	public HediffCompProperties_SolarDisruption()
	{
		compClass = typeof(HediffComp_SolarDisruption);
	}
}
