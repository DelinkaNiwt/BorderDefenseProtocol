using HarmonyLib;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Map), "MapUpdate")]
public static class Patch_Map_MapUpdate
{
	public static void Postfix(Map __instance)
	{
		ATFieldManager.Get(__instance)?.DrawAllFields();
	}
}
