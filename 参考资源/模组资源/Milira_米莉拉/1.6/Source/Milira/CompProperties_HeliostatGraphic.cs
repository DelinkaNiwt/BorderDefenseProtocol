using Verse;

namespace Milira;

public class CompProperties_HeliostatGraphic : CompProperties
{
	public GraphicData graphicData;

	public AltitudeLayer altitudeLayer = AltitudeLayer.BuildingOnTop;

	public CompProperties_HeliostatGraphic()
	{
		compClass = typeof(CompHeliostatGraphic);
	}
}
