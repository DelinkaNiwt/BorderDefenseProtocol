using Verse;

namespace Milira;

public class CompProperties_PromotionGraphic : CompProperties
{
	public bool drawAdditionalGraphicDefault = true;

	public GraphicData graphicData;

	public float floatAmplitude = 0f;

	public float floatSpeed = 0f;

	public float flickerSpeed = 0f;

	public AltitudeLayer altitudeLayer = AltitudeLayer.BuildingOnTop;

	public CompProperties_PromotionGraphic()
	{
		compClass = typeof(CompPromotionGraphic);
	}
}
