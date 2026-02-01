using HarmonyLib;
using RimWorld;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn_PsychicEntropyTracker), "RechargePsyfocus")]
public static class Pawn_EntropyTracker_RechargePsyfocus_Postfix
{
	[HarmonyPrefix]
	public static void Prefix(Pawn_PsychicEntropyTracker __instance)
	{
		__instance.GainXpFromPsyfocus(1f - __instance.CurrentPsyfocus);
	}
}
