using Verse;

namespace Milira;

public class CompProperties_Generator_SunLightFuel : CompProperties
{
	public int productPerGenBase = 1;

	public ThingDef product;

	public CompProperties_Generator_SunLightFuel()
	{
		compClass = typeof(CompGenerator_SunLightFuel);
	}
}
