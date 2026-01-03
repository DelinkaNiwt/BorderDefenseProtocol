using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(CompAbilityEffect_ConsumeLeap))]
[HarmonyPatch("AICanTargetNow")]
public static class Milian_CompAbilityEffect_ConsumeLeap_Patch
{
	[HarmonyPostfix]
	public static bool Prefix(LocalTargetInfo target, ref bool __result)
	{
		Pawn pawn = target.Pawn;
		if (pawn != null && MilianUtility.IsMilian(pawn))
		{
			__result = false;
			return false;
		}
		return true;
	}
}
