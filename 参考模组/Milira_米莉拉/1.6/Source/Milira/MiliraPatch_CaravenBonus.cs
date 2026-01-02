using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(CaravanBonusUtility))]
[HarmonyPatch("HasCaravanBonus")]
public static class MiliraPatch_CaravenBonus
{
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn, ref bool __result)
	{
		if (!__result && MilianUtility.IsMilian(pawn) && !pawn.Downed)
		{
			__result = pawn.GetStatValue(StatDefOf.CaravanBonusSpeedFactor) > 1f;
		}
	}
}
