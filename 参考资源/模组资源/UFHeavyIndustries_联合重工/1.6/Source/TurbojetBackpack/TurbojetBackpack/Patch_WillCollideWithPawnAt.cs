using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_PathFollower), "WillCollideWithPawnAt")]
public static class Patch_WillCollideWithPawnAt
{
	public static bool Prefix(Pawn_PathFollower __instance, IntVec3 c, bool forceOnlyStanding, bool useId, ref bool __result, Pawn ___pawn)
	{
		if (___pawn != null && ___pawn.Map != null && TurbojetGlobal.IsFlightActive(___pawn))
		{
			__result = false;
			return false;
		}
		return true;
	}
}
