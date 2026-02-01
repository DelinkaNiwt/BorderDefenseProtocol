using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NCL_Storyteller;

[HarmonyPatch]
internal class Patch_PawnsArrivalModeWorker
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		List<MethodBase> list = new List<MethodBase>();
		foreach (Type item in typeof(PawnsArrivalModeWorker).AllSubclassesNonAbstract())
		{
			MethodInfo method = item.GetMethod("Arrive", BindingFlags.Instance | BindingFlags.Public);
			if (!(method == null) && method.IsOverriden())
			{
				list.Add(method);
			}
		}
		return list;
	}

	private static bool Prefix(PawnsArrivalModeWorker __instance, List<Pawn> pawns, IncidentParms parms)
	{
		int num = NCL_StorytellerUtility.MaxPawn();
		if (pawns.Count > num)
		{
			pawns.RemoveRange(num, pawns.Count - num);
		}
		return true;
	}
}
