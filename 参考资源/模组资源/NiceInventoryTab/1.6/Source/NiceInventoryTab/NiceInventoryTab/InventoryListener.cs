using HarmonyLib;
using Verse;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(Pawn_InventoryTracker), "Notify_ItemRemoved")]
public static class InventoryListener
{
	public static void Postfix()
	{
		FloatRef.ClearValues();
		ITab_Pawn_Gear_Patch.shouldRecache = true;
	}
}
