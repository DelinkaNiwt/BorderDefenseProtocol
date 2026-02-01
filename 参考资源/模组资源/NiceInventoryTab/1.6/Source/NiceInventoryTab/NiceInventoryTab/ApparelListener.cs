using HarmonyLib;
using RimWorld;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelChanged")]
public static class ApparelListener
{
	public static void Postfix()
	{
		FloatRef.ClearValues();
		ITab_Pawn_Gear_Patch.shouldRecache = true;
	}
}
