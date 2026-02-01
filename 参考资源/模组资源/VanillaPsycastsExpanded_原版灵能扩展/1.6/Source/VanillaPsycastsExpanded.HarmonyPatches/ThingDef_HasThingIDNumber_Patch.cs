using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded.HarmonyPatches;

[HarmonyPatch(typeof(ThingDef), "HasThingIDNumber", MethodType.Getter)]
public static class ThingDef_HasThingIDNumber_Patch
{
	public static void Postfix(ThingDef __instance, ref bool __result)
	{
		if (__instance.CanBeSaved())
		{
			__result = true;
		}
	}
}
