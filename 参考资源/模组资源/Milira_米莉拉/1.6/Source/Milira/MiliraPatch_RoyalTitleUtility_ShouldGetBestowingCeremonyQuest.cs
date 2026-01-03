using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(RoyalTitleUtility))]
[HarmonyPatch("ShouldGetBestowingCeremonyQuest")]
[HarmonyPatch(new Type[]
{
	typeof(Pawn),
	typeof(Faction)
})]
public static class MiliraPatch_RoyalTitleUtility_ShouldGetBestowingCeremonyQuest
{
	[HarmonyPrefix]
	public static bool Prefix(Pawn pawn, Faction faction, ref bool __result)
	{
		if (faction.def == MiliraDefOf.Milira_AngelismChurch)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
