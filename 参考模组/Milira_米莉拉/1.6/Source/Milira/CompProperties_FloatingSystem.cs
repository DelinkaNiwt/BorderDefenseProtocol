using Verse;

namespace Milira;

public class CompProperties_FloatingSystem : CompProperties
{
	public GraphicData graphicData;

	public float floatAmplitude = 0f;

	public float floatSpeed = 0f;

	public AltitudeLayer altitudeLayer = AltitudeLayer.BuildingOnTop;

	public CompProperties_FloatingSystem()
	{
		compClass = typeof(CompFloatingSystem);
	}
}
