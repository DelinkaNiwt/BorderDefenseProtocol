using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace GD3
{
	[HarmonyPatch(typeof(Building), "SetFaction")]
	public static class DeckReinforce_Patch
	{
		public static void Postfix(Building __instance, Faction newFaction)
		{
			if (newFaction != Faction.OfPlayer)
            {
				__instance.TryGetComp<CompDeckReinforce>()?.ChangeState(false);
            }
		}
	}
}
