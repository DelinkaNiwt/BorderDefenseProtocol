using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch]
public static class Toils_Ingest_FinalizeIngest_Patch
{
	[HarmonyTargetMethod]
	public static MethodBase GetMethod()
	{
		Type[] nestedTypes = typeof(Toils_Ingest).GetNestedTypes(AccessTools.all);
		for (int i = 0; i < nestedTypes.Length; i++)
		{
			MethodInfo methodInfo = nestedTypes[i].GetMethods(AccessTools.all).FirstOrDefault((MethodInfo x) => x.Name.Contains("<FinalizeIngest>"));
			if (methodInfo != null)
			{
				return methodInfo;
			}
		}
		throw new Exception("Toils_Ingest_FinalizeIngest_Patch failed to find a method to patch.");
	}

	public static void Prefix(object __instance)
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = Traverse.Create(__instance).Field("ingester").GetValue<Pawn>();
	}

	public static void Postfix()
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = null;
	}
}
