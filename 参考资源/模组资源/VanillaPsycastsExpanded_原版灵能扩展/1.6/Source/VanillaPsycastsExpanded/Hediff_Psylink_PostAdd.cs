using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Hediff_Psylink), "PostAdd")]
public static class Hediff_Psylink_PostAdd
{
	public static void Postfix(Hediff_Psylink __instance)
	{
		((Hediff_PsycastAbilities)(object)__instance.pawn.health.AddHediff(VPE_DefOf.VPE_PsycastAbilityImplant, __instance.Part)).InitializeFromPsylink(__instance);
	}
}
