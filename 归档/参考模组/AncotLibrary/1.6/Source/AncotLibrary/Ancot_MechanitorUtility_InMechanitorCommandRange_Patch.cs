using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(MechanitorUtility))]
[HarmonyPatch("InMechanitorCommandRange")]
public static class Ancot_MechanitorUtility_InMechanitorCommandRange_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(100)]
	public static bool Prefix(Pawn mech, LocalTargetInfo target, ref bool __result)
	{
		if (!__result)
		{
			__result = mech.TryGetComp<CompDraftable>()?.Draftable ?? false;
			if (!__result)
			{
				return true;
			}
			return false;
		}
		return true;
	}
}
