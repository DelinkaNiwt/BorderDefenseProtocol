using Verse;

namespace Milira;

public class CompProperties_SunBlasterFurnaceHeatPusher : CompProperties
{
	public float heatPerSecond;

	public float heatPushMaxTemperature = 99999f;

	public float heatPushMinTemperature = -99999f;

	public bool onlyHeatWhenConsumingFuel = true;

	public CompProperties_SunBlasterFurnaceHeatPusher()
	{
		compClass = typeof(CompSunBlasterFurnaceHeatPusher);
	}
}
