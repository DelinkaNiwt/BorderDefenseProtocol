using AncotLibrary;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WeaponFitting;

[HarmonyPatch(typeof(CompUniqueWeapon))]
[HarmonyPatch("Notify_EquipmentLost")]
public static class UniqueWeaponLostPatch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn, CompUniqueWeapon __instance)
	{
		if (!__instance.IsTraitsEmpty())
		{
			__instance.parent.AddToGC();
		}
	}
}
