using HarmonyLib;
using Verse;

namespace FloatSubMenus;

[StaticConstructorOnStartup]
[HarmonyPatch]
internal static class Patches
{
	private static bool replaceDist;

	private static float dist;

	static Patches()
	{
		new Harmony("kathanon.FloatSubMenu").PatchAll();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(FloatMenu), "UpdateBaseColor")]
	public static void UpdateBaseColor_Pre(FloatMenu __instance)
	{
		replaceDist = false;
		replaceDist = FloatSubMenu.ShouldReplaceDistanceFor(__instance, ref dist);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(FloatMenu), "UpdateBaseColor")]
	public static void UpdateBaseColor_Post()
	{
		replaceDist = false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GenUI), "DistFromRect")]
	public static bool DistFromRect_Pre(ref float __result)
	{
		if (replaceDist)
		{
			__result = dist;
			return false;
		}
		return true;
	}
}
