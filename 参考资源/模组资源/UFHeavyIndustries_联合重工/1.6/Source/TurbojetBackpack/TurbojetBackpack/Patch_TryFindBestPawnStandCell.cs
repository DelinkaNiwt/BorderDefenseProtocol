using HarmonyLib;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(CellFinder), "TryFindBestPawnStandCell")]
public static class Patch_TryFindBestPawnStandCell
{
	public static bool Prefix(Pawn forPawn, out IntVec3 cell, ref bool __result)
	{
		cell = IntVec3.Invalid;
		if (forPawn != null && forPawn.Map != null && TurbojetGlobal.IsFlightActive(forPawn))
		{
			__result = false;
			return false;
		}
		return true;
	}
}
