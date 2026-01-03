using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(CompAbilityEffect_DeactivateMechanoid), "Valid")]
public static class Milian_CompAbilityEffect_DeactivateMechanoid_Valid_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ref bool __result, LocalTargetInfo target, bool throwMessages = false)
	{
		if (__result)
		{
			Pawn pawn = target.Pawn;
			if (MilianUtility.IsMilian(pawn))
			{
				__result = false;
			}
		}
	}
}
