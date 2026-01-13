using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_PathFollower), "TryRecoverFromUnwalkablePosition", new Type[] { typeof(bool) })]
public static class Patch_PreventBounce
{
	public static bool Prefix(bool error, Pawn ___pawn)
	{
		if (___pawn != null && ___pawn.Map != null && TurbojetGlobal.CanPassCell(___pawn, ___pawn.Map, ___pawn.Position))
		{
			return false;
		}
		return true;
	}
}
