using HarmonyLib;
using RimWorld;
using Verse;

namespace MechanitorCommandRange;

[HarmonyPatch(typeof(Pawn_MechanitorTracker), "CanCommandTo")]
internal static class PatchMechanitorTracker_CanCommandTo
{
	private static bool Prefix(Pawn_MechanitorTracker __instance, LocalTargetInfo target, ref bool __result)
	{
		if (__instance.Pawn.health.hediffSet.HasHediff(HediffDef.Named("NCL_King_of_the_field")))
		{
			__result = true;
			return false;
		}
		return true;
	}
}
