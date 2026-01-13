using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(SkipUtility), "SkipTo")]
public static class Patch_SkipUtility_SkipTo
{
	public static void Prefix(Thing thing, ref IntVec3 cell, Map dest)
	{
		if (thing != null && dest != null && cell.IsValid && thing is Pawn)
		{
			IntVec3 intVec = ATFieldInterceptUtility.TryInterceptTeleportDestination(dest, cell, thing);
			if (intVec != cell)
			{
				cell = intVec;
			}
		}
	}
}
