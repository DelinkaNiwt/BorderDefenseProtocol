using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(MeditationFocusDef), "EnablingThingsExplanation")]
public static class MeditationFocusDef_EnablingThingsExplanation_Patch
{
	public static void Postfix(Pawn pawn, MeditationFocusDef __instance, ref string __result)
	{
		Hediff_PsycastAbilities hediff_PsycastAbilities = pawn.Psycasts();
		if (hediff_PsycastAbilities != null && hediff_PsycastAbilities.unlockedMeditationFoci.Contains(__instance))
		{
			__result += "\n  - " + "VPE.UnlockedByPoints".Translate() + ".";
		}
	}
}
