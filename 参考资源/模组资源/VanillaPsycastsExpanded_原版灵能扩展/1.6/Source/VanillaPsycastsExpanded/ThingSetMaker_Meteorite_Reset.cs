using HarmonyLib;
using RimWorld;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(ThingSetMaker_Meteorite), "Reset")]
public static class ThingSetMaker_Meteorite_Reset
{
	public static void Postfix()
	{
		ThingSetMaker_Meteorite.nonSmoothedMineables.Remove(VPE_DefOf.VPE_EltexOre);
	}
}
