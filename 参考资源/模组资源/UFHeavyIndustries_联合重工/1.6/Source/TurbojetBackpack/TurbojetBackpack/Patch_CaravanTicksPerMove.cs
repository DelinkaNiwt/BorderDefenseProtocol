using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(CaravanTicksPerMoveUtility), "GetTicksPerMove", new Type[]
{
	typeof(List<Pawn>),
	typeof(float),
	typeof(float),
	typeof(bool),
	typeof(StringBuilder)
})]
public static class Patch_CaravanTicksPerMove
{
	public static void Postfix(List<Pawn> pawns, ref int __result)
	{
		if (pawns.NullOrEmpty())
		{
			return;
		}
		float num = 0f;
		int count = pawns.Count;
		for (int i = 0; i < count; i++)
		{
			Pawn pawn = pawns[i];
			float num2 = 1f;
			if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0)
			{
				List<Apparel> wornApparel = pawn.apparel.WornApparel;
				for (int j = 0; j < wornApparel.Count; j++)
				{
					TurbojetExtension modExtension = wornApparel[j].def.GetModExtension<TurbojetExtension>();
					if (modExtension != null && modExtension.worldMapSpeedFactor > 1f && modExtension.worldMapSpeedFactor > num2)
					{
						num2 = modExtension.worldMapSpeedFactor;
					}
				}
			}
			num += num2;
		}
		if (num > (float)count)
		{
			float num3 = num / (float)count;
			if (num3 > 1.001f)
			{
				__result = Mathf.RoundToInt((float)__result / num3);
			}
		}
	}
}
