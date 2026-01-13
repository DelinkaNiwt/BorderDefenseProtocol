using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_PathFollower), "PawnCanOccupy", new Type[] { typeof(IntVec3) })]
public static class Patch_PawnCanOccupy
{
	public static bool Prefix(IntVec3 c, Pawn ___pawn, ref bool __result)
	{
		if (___pawn != null && ___pawn.Map != null && !c.WalkableBy(___pawn.Map, ___pawn) && TurbojetGlobal.CanPassCell(___pawn, ___pawn.Map, c))
		{
			__result = true;
			return false;
		}
		return true;
	}
}
