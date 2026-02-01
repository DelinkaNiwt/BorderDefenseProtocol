using RimWorld;
using Verse;

namespace NCL;

[DefOf]
public static class TameMechefOf
{
	public static JobDef TW_TameMech;

	public static DesignationDef TW_TameMechDesignation;

	static TameMechefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TameMechefOf));
	}
}
