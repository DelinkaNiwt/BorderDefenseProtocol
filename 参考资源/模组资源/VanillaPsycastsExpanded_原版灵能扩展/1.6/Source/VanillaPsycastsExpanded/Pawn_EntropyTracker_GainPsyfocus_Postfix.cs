using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn_PsychicEntropyTracker), "GainPsyfocus_NewTemp")]
public static class Pawn_EntropyTracker_GainPsyfocus_Postfix
{
	public static void Postfix(Pawn_PsychicEntropyTracker __instance, int delta, Thing focus = null)
	{
		float gain = MeditationUtility.PsyfocusGainPerTick(__instance.Pawn, focus) * (float)delta;
		__instance.GainXpFromPsyfocus(gain);
	}

	public static void GainXpFromPsyfocus(this Pawn_PsychicEntropyTracker __instance, float gain)
	{
		__instance.Pawn?.Psycasts()?.GainExperience(gain * 100f * PsycastsMod.Settings.XPPerPercent);
	}
}
