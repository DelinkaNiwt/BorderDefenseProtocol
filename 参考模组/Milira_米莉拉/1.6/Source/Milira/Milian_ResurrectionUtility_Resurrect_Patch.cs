using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(ResurrectionUtility))]
[HarmonyPatch("TryResurrect")]
public static class Milian_ResurrectionUtility_Resurrect_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn)
	{
		if (MilianUtility.IsMilian(pawn) && pawn.Faction.IsPlayer && !MechRepairUtility.IsMissingWeapon(pawn))
		{
			pawn.equipment.DestroyAllEquipment();
		}
	}
}
