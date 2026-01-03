using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(UnfinishedThing), "get_LabelNoCount")]
public static class AncotPatch_UnfinishedThingLabel
{
	[HarmonyPrefix]
	public static bool Prefix(ref string __result, UnfinishedThing __instance)
	{
		if (__instance.Recipe == null)
		{
			__result = GenLabel.ThingLabel(__instance, 1);
			return false;
		}
		if (__instance.Recipe.products.NullOrEmpty())
		{
			__result = GenLabel.ThingLabel(__instance, 1) + "(" + __instance.Recipe.label + ")";
			return false;
		}
		return true;
	}
}
