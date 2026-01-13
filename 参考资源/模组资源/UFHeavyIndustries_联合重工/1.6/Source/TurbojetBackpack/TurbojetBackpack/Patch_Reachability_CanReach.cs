using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Reachability), "CanReach", new Type[]
{
	typeof(IntVec3),
	typeof(LocalTargetInfo),
	typeof(PathEndMode),
	typeof(TraverseParms)
})]
public static class Patch_Reachability_CanReach
{
	public static bool Prefix(ref bool __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParams, Reachability __instance)
	{
		if (TurbojetGlobal.SkipReachabilityCheck)
		{
			__result = true;
			return false;
		}
		Pawn pawn = traverseParams.pawn;
		if (pawn != null && pawn.Map != null && TurbojetGlobal.IsFlightActive(pawn) && dest.Cell.InBounds(pawn.Map))
		{
			if (!TurbojetGlobal.IsValidDestination(pawn, pawn.Map, dest.Cell))
			{
				__result = false;
				return false;
			}
			__result = true;
			return false;
		}
		return true;
	}
}
