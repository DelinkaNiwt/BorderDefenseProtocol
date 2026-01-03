using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(TransportersArrivalActionUtility))]
[HarmonyPatch("AnyNonDownedColonist")]
public static class Milian_Arriavl_fix
{
	[HarmonyPostfix]
	public static void Postfix(ref bool __result, IEnumerable<IThingHolder> pods)
	{
		if (__result)
		{
			return;
		}
		foreach (IThingHolder pod in pods)
		{
			ThingOwner directlyHeldThings = pod.GetDirectlyHeldThings();
			for (int i = 0; i < directlyHeldThings.Count; i++)
			{
				Pawn pawn = directlyHeldThings[i] as Pawn;
				if (MilianUtility.IsMilian(pawn) && !pawn.Downed)
				{
					__result = true;
					return;
				}
			}
		}
	}
}
