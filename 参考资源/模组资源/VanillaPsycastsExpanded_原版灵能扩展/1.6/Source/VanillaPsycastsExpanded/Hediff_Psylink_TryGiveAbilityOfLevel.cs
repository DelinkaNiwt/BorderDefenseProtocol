using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Hediff_Psylink), "TryGiveAbilityOfLevel")]
public static class Hediff_Psylink_TryGiveAbilityOfLevel
{
	public static bool Prefix()
	{
		return false;
	}
}
