using Verse;

namespace Milira;

public class CompProperties_ClassAmplificationLoop : CompProperties
{
	public EffecterDef effecter;

	public int amplificationTickDuration = 1200;

	public int checkInterval = 60;

	public GraphicData graphicData;

	public float floatAmplitude = 0f;

	public float floatSpeed = 0f;

	public AltitudeLayer altitudeLayer = AltitudeLayer.BuildingOnTop;

	public CompProperties_ClassAmplificationLoop()
	{
		compClass = typeof(CompClassAmplificationLoop);
	}
}
