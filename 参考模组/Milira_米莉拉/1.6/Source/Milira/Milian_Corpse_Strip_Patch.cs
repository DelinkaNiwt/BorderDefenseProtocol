using HarmonyLib;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Corpse))]
[HarmonyPatch("Strip")]
public static class Milian_Corpse_Strip_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Corpse __instance)
	{
		Pawn innerPawn = __instance.InnerPawn;
		if (innerPawn != null && MilianUtility.IsMilian(innerPawn) && !innerPawn.Faction.IsPlayer)
		{
			return false;
		}
		return true;
	}
}
