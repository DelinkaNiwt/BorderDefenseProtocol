using RimWorld;
using Verse;

namespace TOT_DLL_test;

[DefOf]
public static class EffecterDefOf
{
	public static EffecterDef CMC_AABomb;

	static EffecterDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(EffecterDefOf));
	}
}
