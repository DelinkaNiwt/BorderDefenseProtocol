using HarmonyLib;
using RimWorld;

namespace Milira;

[HarmonyPatch(typeof(IncidentWorker_Raid))]
[HarmonyPatch("AdjustedRaidPoints")]
public static class Milian_IncidentWorker_Raid_AdjustedRaidPoints_Patch
{
	[HarmonyPostfix]
	public static void Postfix(float points, PawnsArrivalModeDef raidArrivalMode, RaidStrategyDef raidStrategy, Faction faction, PawnGroupKindDef groupKind, RaidAgeRestrictionDef ageRestriction, ref float __result)
	{
		if (faction != null && faction.def == MiliraDefOf.Milira_Faction)
		{
			__result *= MiliraRaceSettings.DifficultyScale(MiliraRaceSettings.currentGameDifficulty);
		}
	}
}
