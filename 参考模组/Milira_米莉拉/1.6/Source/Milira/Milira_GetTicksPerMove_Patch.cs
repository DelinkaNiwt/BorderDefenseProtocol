using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Milira;

[HarmonyPatch(new Type[]
{
	typeof(List<Pawn>),
	typeof(float),
	typeof(float),
	typeof(bool),
	typeof(StringBuilder)
})]
[HarmonyPatch(typeof(CaravanTicksPerMoveUtility))]
[HarmonyPatch("GetTicksPerMove")]
public static class Milira_GetTicksPerMove_Patch
{
	[HarmonyPostfix]
	public static void Postfix(List<Pawn> pawns, ref int __result, float massUsage, float massCapacity, StringBuilder explanation = null)
	{
		if (pawns.All((Pawn pawn) => pawn.def.defName == "Milira_Race"))
		{
			__result /= 20;
			explanation?.AppendLine("\n" + "Milira_CaravanTravelExplanation".Translate());
		}
	}
}
