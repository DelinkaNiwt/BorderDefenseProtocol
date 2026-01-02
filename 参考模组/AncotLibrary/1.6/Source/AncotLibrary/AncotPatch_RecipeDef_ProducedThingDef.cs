using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(RecipeDef))]
[HarmonyPatch("ProducedThingDef", MethodType.Getter)]
public static class AncotPatch_RecipeDef_ProducedThingDef
{
	[HarmonyPrefix]
	public static bool Prefix(ref ThingDef __result, RecipeDef __instance)
	{
		if (__instance.products.NullOrEmpty())
		{
			ModExtension_AssembleDrone modExtension = __instance.GetModExtension<ModExtension_AssembleDrone>();
			if (modExtension != null)
			{
				__result = modExtension.droneKind.race;
			}
			return false;
		}
		return true;
	}
}
