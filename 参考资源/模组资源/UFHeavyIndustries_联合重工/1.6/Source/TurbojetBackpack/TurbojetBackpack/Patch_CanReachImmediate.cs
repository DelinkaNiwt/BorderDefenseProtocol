using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(ReachabilityImmediate), "CanReachImmediate", new Type[]
{
	typeof(IntVec3),
	typeof(LocalTargetInfo),
	typeof(Map),
	typeof(PathEndMode),
	typeof(Pawn)
})]
public static class Patch_CanReachImmediate
{
	public static bool Prefix(IntVec3 start, LocalTargetInfo target, Map map, PathEndMode peMode, Pawn pawn, ref bool __result)
	{
		if (pawn != null && TurbojetGlobal.IsFlightActive(pawn) && !target.HasThing && start == target.Cell)
		{
			__result = true;
			return false;
		}
		return true;
	}
}
