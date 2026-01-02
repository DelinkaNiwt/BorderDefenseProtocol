using HarmonyLib;
using RimWorld.Planet;

namespace AncotLibrary;

[HarmonyPatch(typeof(TravellingTransporters))]
[HarmonyPatch("TraveledPctStepPerTick", MethodType.Getter)]
public static class AncotPatch_TravellingTransporters_TraveledPctStepPerTick
{
	[HarmonyPostfix]
	public static void Postfix(ref float __result, TravellingTransporters __instance)
	{
		WorldObjectShuttle_Extension modExtension = __instance.def.GetModExtension<WorldObjectShuttle_Extension>();
		if (modExtension != null)
		{
			__result *= modExtension.traveledPctStepPerTickFactor;
		}
	}
}
