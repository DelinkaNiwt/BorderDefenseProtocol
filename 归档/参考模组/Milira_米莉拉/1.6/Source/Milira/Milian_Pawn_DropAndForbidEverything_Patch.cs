using HarmonyLib;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Pawn))]
[HarmonyPatch("DropAndForbidEverything")]
public static class Milian_Pawn_DropAndForbidEverything_Patch
{
	[HarmonyPrefix]
	public static void Prefix(Pawn __instance)
	{
		if (MilianUtility.IsMilian(__instance) && !__instance.Faction.IsPlayer)
		{
			__instance.equipment.DestroyAllEquipment();
		}
	}
}
