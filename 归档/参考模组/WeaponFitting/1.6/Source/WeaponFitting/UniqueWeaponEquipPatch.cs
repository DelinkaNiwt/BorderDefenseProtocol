using AncotLibrary;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WeaponFitting;

[HarmonyPatch(typeof(CompUniqueWeapon))]
[HarmonyPatch("Notify_Equipped")]
public static class UniqueWeaponEquipPatch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn, CompUniqueWeapon __instance)
	{
		if (pawn.Faction.IsPlayerSafe() && !__instance.IsTraitsEmpty())
		{
			__instance.parent.AddToGC();
		}
	}
}
