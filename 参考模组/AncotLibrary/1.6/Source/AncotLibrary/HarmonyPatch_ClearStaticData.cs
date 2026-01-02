using HarmonyLib;
using Verse.Profile;

namespace AncotLibrary;

[HarmonyPatch]
public static class HarmonyPatch_ClearStaticData
{
	[HarmonyPatch(typeof(MemoryUtility), "ClearAllMapsAndWorld")]
	[HarmonyPostfix]
	public static void ClearStaticData()
	{
		Alert_MechAutoFight.ClearCache();
	}
}
