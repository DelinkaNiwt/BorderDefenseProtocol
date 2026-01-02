using RimWorld;
using Verse;

namespace MYDE_CMC_Dll;

[DefOf]
public static class MYDE_ThingDefOf
{
	public static ThingDef CMC_DECO_WIFI;

	static MYDE_ThingDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(MYDE_ThingDefOf));
	}
}
