using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(Projectile))]
[HarmonyPatch("DamageAmount", MethodType.Getter)]
public static class Projectile_DamageAmount_Patch
{
	public static void Postfix(ref int __result, Projectile __instance)
	{
		Pawn pawn = __instance.Launcher as Pawn;
		float num = 1f;
		if (pawn != null)
		{
			num = pawn.GetStatValue(AncotDefOf.Ancot_ProjectileDamageMultiplier);
		}
		__result = Mathf.RoundToInt((float)__result * num);
	}
}
