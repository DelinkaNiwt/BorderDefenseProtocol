using HarmonyLib;
using RimWorld;
using Verse;

namespace MechanitorCommandRange;

[HarmonyPatch(typeof(Pawn_MechanitorTracker), "DrawCommandRadius")]
internal static class PatchMechanitorTracker_DrawCommandRadius
{
	private static bool Prefix(Pawn_MechanitorTracker __instance)
	{
		if (__instance.Pawn.health.hediffSet.HasHediff(HediffDef.Named("NCL_King_of_the_field")))
		{
			return false;
		}
		return true;
	}
}
