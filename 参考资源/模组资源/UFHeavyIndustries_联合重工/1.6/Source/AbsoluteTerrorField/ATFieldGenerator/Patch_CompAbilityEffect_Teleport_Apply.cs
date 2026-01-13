using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(CompAbilityEffect_Teleport), "Apply")]
public static class Patch_CompAbilityEffect_Teleport_Apply
{
	public static void Prefix(CompAbilityEffect_Teleport __instance, ref LocalTargetInfo target, ref LocalTargetInfo dest)
	{
		Pawn pawn = __instance.parent?.pawn;
		if (pawn == null || !dest.IsValid)
		{
			return;
		}
		Map map = pawn.Map;
		if (map == null)
		{
			return;
		}
		if (target.HasThing && target.Thing is Pawn { Map: not null } pawn2 && ATFieldInterceptUtility.CheckAbductionAttempt(pawn, pawn2))
		{
			dest = new LocalTargetInfo(pawn2.Position);
			return;
		}
		Thing thing = (target.HasThing ? target.Thing : pawn);
		if (thing is Pawn)
		{
			IntVec3 cell = dest.Cell;
			IntVec3 intVec = ATFieldInterceptUtility.TryInterceptTeleportDestination(map, cell, thing);
			if (intVec != cell)
			{
				dest = new LocalTargetInfo(intVec);
			}
		}
	}
}
