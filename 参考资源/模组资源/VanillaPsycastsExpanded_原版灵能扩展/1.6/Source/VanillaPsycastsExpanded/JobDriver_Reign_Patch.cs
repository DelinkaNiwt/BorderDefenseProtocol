using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace VanillaPsycastsExpanded;

[HarmonyPatch]
public static class JobDriver_Reign_Patch
{
	[HarmonyTargetMethod]
	public static MethodBase GetMethod()
	{
		return typeof(JobDriver_Reign).GetMethods(AccessTools.all).Last((MethodInfo x) => x.Name.Contains("<MakeNewToils>"));
	}

	public static void Prefix(JobDriver_Reign __instance)
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = __instance.pawn;
	}

	public static void Postfix()
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = null;
	}
}
