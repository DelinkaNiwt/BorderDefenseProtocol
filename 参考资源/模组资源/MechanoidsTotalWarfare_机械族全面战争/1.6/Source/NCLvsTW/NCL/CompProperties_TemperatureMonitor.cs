using Verse;

namespace NCL;

public class CompProperties_TemperatureMonitor : CompProperties
{
	public float warningTemperature = 80f;

	public float criticalTemperature = 120f;

	public int checkInterval = 250;

	public CompProperties_TemperatureMonitor()
	{
		compClass = typeof(CompTemperatureMonitor);
	}
}
