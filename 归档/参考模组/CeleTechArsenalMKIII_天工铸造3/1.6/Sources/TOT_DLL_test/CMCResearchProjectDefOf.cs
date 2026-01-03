using RimWorld;
using Verse;

namespace TOT_DLL_test;

[DefOf]
public static class CMCResearchProjectDefOf
{
	public static ResearchProjectDef CMCGunTurrets;

	public static ResearchProjectDef CMC_Smart_I;

	static CMCResearchProjectDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(ResearchProjectDefOf));
	}
}
