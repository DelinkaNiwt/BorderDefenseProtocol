using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GD3
{
	[HarmonyPatch(typeof(Pawn), "Kill")]
	public static class Pawn_Kill_Patch
	{
		public static void Prefix(Pawn __instance, out Map __state)
		{
			__state = __instance.MapHeld;
		}

		public static void Postfix(Pawn __instance, Map __state)
		{
			bool flag = __instance.Dead && __state != null;
			if (flag)
			{
				CompApocriton comp = __instance.GetComp<CompApocriton>();
				bool flag2 = comp != null;
				if (flag2)
				{
					comp.MentalBreakOnKilled(__state);
				}
			}
		}
	}
}
