using Verse;

namespace Milira;

public class CompSunBlasterFurnaceHeatPusher : ThingComp
{
	public CompProperties_SunBlasterFurnaceHeatPusher Props => (CompProperties_SunBlasterFurnaceHeatPusher)props;

	public void HeatPush()
	{
		CompProperties_SunBlasterFurnaceHeatPusher compProperties_SunBlasterFurnaceHeatPusher = Props;
		float ambientTemperature = parent.AmbientTemperature;
		if (ambientTemperature < compProperties_SunBlasterFurnaceHeatPusher.heatPushMaxTemperature && ambientTemperature > compProperties_SunBlasterFurnaceHeatPusher.heatPushMinTemperature)
		{
			GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, Props.heatPerSecond);
		}
	}
}
