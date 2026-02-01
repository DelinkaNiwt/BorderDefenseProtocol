using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MechanitorCommandRange;

[HarmonyPatch(typeof(Pawn_MechanitorTracker), "CanControlMechs", MethodType.Getter)]
internal static class PatchPawn_MechanitorTracker_CanControlMechs_Getter
{
	private static void Postfix(Pawn_MechanitorTracker __instance, ref AcceptanceReport __result)
	{
		if (!__result.Accepted && __instance.Pawn.IsCaravanMember() && __instance.Pawn.health.hediffSet.HasHediff(HediffDef.Named("NCL_King_of_the_field")))
		{
			__result = true;
		}
	}
}
