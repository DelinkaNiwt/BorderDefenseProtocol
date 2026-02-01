using HarmonyLib;
using Verse;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded")]
public static class EquipmentListener
{
	public static void Postfix()
	{
		FloatRef.ClearValues();
		ITab_Pawn_Gear_Patch.shouldRecache = true;
	}
}
