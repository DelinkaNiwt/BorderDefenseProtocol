using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Dialog_FormCaravan), "TryAddCorpseInventoryAndGearToTransferables")]
public static class Milian_Dialog_FormCaravan_TryAddCorpseInventoryAndGearToTransferables_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Thing potentiallyCorpse)
	{
		if (potentiallyCorpse is Corpse corpse && MilianUtility.IsMilian(corpse.InnerPawn))
		{
			return false;
		}
		return true;
	}
}
