using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_PathFollower))]
public static class Patch_Pawn_PathFollower
{
	[HarmonyPatch("StartPath")]
	[HarmonyPrefix]
	public static void Prefix_StartPath(Pawn ___pawn)
	{
		if (___pawn != null && ___pawn.Map != null && TurbojetGlobal.IsFlightActive(___pawn))
		{
			TurbojetGlobal.SkipReachabilityCheck = true;
		}
	}

	[HarmonyPatch("StartPath")]
	[HarmonyPostfix]
	public static void Postfix_StartPath()
	{
		TurbojetGlobal.SkipReachabilityCheck = false;
	}

	[HarmonyPatch("NeedNewPath")]
	[HarmonyPostfix]
	public static void Postfix_NeedNewPath(Pawn ___pawn, ref bool __result, LocalTargetInfo ___destination)
	{
		if (__result && ___destination.IsValid && ___pawn != null && ___pawn.Map != null && TurbojetGlobal.IsFlightActive(___pawn))
		{
			__result = false;
		}
	}

	[HarmonyPatch("TrySetNewPathRequest")]
	[HarmonyPrefix]
	public static bool Prefix_TrySetNewPathRequest(Pawn ___pawn)
	{
		if (___pawn != null && ___pawn.Map != null && TurbojetGlobal.IsFlightActive(___pawn))
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch("BuildingBlockingNextPathCell")]
	[HarmonyPrefix]
	public static bool Prefix_BuildingBlocking(Pawn ___pawn, ref Building __result)
	{
		if (___pawn != null && ___pawn.Map != null && TurbojetGlobal.IsFlightActive(___pawn))
		{
			__result = null;
			return false;
		}
		return true;
	}

	[HarmonyPatch("NextCellDoorToWaitForOrManuallyOpen")]
	[HarmonyPrefix]
	public static bool Prefix_DoorWait(Pawn ___pawn, ref Building_Door __result)
	{
		if (___pawn != null && ___pawn.Map != null && TurbojetGlobal.IsFlightActive(___pawn))
		{
			if (TurbojetGlobal.GetEffectiveMode(___pawn) == TurbojetMode.HoverMoving)
			{
				return true;
			}
			__result = null;
			return false;
		}
		return true;
	}

	[HarmonyPatch("SetupMoveIntoNextCell")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler_Setup(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo originalMethod = AccessTools.Method(typeof(GenGrid), "WalkableBy", new Type[3]
		{
			typeof(IntVec3),
			typeof(Map),
			typeof(Pawn)
		});
		MethodInfo replacementMethod = AccessTools.Method(typeof(Patch_Pawn_PathFollower), "IsWalkableOrFlying");
		bool found = false;
		foreach (CodeInstruction code in instructions)
		{
			if (code.Calls(originalMethod))
			{
				yield return new CodeInstruction(OpCodes.Call, replacementMethod);
				found = true;
			}
			else
			{
				yield return code;
			}
		}
		if (!found)
		{
			Log.Error("[Turbojet] Failed to patch WalkableBy in SetupMoveIntoNextCell");
		}
	}

	public static bool IsWalkableOrFlying(IntVec3 cell, Map map, Pawn pawn)
	{
		if (cell.WalkableBy(map, pawn))
		{
			return true;
		}
		if (TurbojetGlobal.CanPassCell(pawn, map, cell))
		{
			return true;
		}
		return false;
	}
}
