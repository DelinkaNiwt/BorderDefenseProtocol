using HarmonyLib;
using RimWorld;
using Verse;

namespace Edited_BM_WeaponSummon;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
public static class PreventWeaponDrop_Patch
{
	private static bool Prefix(ThingWithComps eq, out ThingWithComps resultingEq, ref bool __result)
	{
		resultingEq = null;
		if (eq != null && eq.GetComp<CompPreventDrop>() != null)
		{
			Messages.Message("CannotDropBoundWeapon".Translate(), MessageTypeDefOf.RejectInput);
			__result = false;
			return false;
		}
		return true;
	}
}
