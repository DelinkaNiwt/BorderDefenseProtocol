using Verse;

namespace Milira;

public class CompProperties_SunBlastFurnaceIllegalUse : CompProperties
{
	public int ticksPerPoint = 1000;

	public CompProperties_SunBlastFurnaceIllegalUse()
	{
		compClass = typeof(CompSunBlastFurnaceIllegalUse);
	}
}
