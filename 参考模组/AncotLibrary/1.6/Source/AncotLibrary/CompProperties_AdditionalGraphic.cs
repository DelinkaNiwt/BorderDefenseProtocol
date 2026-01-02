using Verse;

namespace AncotLibrary;

public class CompProperties_AdditionalGraphic : CompProperties
{
	public bool drawAdditionalGraphicDefault = true;

	public bool drawOnlyDrafted = false;

	public GraphicData graphicData;

	public float floatAmplitude = 0f;

	public float floatSpeed = 0f;

	public FloatRange sizeTwinkle = new FloatRange(0f, 0f);

	public float sizeTwinkleSpeed = 0f;

	public AltitudeLayer altitudeLayer = AltitudeLayer.BuildingOnTop;

	public CompProperties_AdditionalGraphic()
	{
		compClass = typeof(CompAdditionalGraphic);
	}
}
