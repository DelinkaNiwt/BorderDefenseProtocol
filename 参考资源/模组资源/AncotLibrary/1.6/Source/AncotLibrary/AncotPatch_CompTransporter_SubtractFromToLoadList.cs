using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(CompTransporter))]
[HarmonyPatch("SubtractFromToLoadList")]
public static class AncotPatch_CompTransporter_SubtractFromToLoadList
{
	[HarmonyPrefix]
	public static void Prefix(CompTransporter __instance, Thing t, int count, ref bool sendMessageOnFinished)
	{
		if (__instance is CompTransporterCustom)
		{
			sendMessageOnFinished = false;
		}
	}
}
