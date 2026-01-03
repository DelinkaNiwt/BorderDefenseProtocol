using Verse;

namespace AncotLibrary;

public class CompProperties_CombatPlatform : CompProperties
{
	public FloatGraph float_yAxis;

	public FloatGraph float_xAxis;

	public GraphicData graphicData;

	public CompProperties_CombatPlatform()
	{
		compClass = typeof(CompCombatPlatform);
	}
}
