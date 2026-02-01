using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(HediffSet), "DirtyCache")]
public static class HediffSet_DirtyCache_Patch
{
	public static void Postfix(HediffSet __instance)
	{
		__instance.pawn.RecheckPaths();
	}
}
