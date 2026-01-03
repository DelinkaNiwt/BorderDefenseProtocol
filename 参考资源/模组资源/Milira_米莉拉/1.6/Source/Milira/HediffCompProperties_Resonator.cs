using Verse;

namespace Milira;

public class HediffCompProperties_Resonator : HediffCompProperties
{
	[NoTranslate]
	public string resonatorTag;

	public HediffCompProperties_Resonator()
	{
		compClass = typeof(HediffComp_Resonator);
	}
}
