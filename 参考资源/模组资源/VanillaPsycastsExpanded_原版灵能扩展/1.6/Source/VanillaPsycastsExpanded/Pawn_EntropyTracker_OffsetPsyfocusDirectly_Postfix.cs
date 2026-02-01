using HarmonyLib;
using RimWorld;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn_PsychicEntropyTracker), "OffsetPsyfocusDirectly")]
public static class Pawn_EntropyTracker_OffsetPsyfocusDirectly_Postfix
{
	[HarmonyPostfix]
	public static void Postfix(Pawn_PsychicEntropyTracker __instance, float offset)
	{
		if (offset > 0f)
		{
			__instance.GainXpFromPsyfocus(offset);
		}
	}
}
