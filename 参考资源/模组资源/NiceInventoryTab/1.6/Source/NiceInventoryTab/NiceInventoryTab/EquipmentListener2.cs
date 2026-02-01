using HarmonyLib;
using Verse;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_EquipmentRemoved")]
public static class EquipmentListener2
{
	public static void Postfix()
	{
		FloatRef.ClearValues();
		ITab_Pawn_Gear_Patch.shouldRecache = true;
	}
}
