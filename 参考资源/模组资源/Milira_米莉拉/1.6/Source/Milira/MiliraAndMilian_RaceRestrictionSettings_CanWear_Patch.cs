using AlienRace;
using HarmonyLib;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(RaceRestrictionSettings), "CanWear")]
public static class MiliraAndMilian_RaceRestrictionSettings_CanWear_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(ThingDef apparel, ThingDef race, ref bool __result)
	{
		if (race.defName == "Milira_Race" && !MiliraRaceSettings.MiliraRace_ModSetting_RaceRestrictedApparel)
		{
			__result = true;
			return false;
		}
		if (!__result && MilianUtility.IsMilian(race))
		{
			__result = true;
			return false;
		}
		return true;
	}
}
