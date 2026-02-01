using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

[HarmonyPatch(typeof(HediffComp_ReactOnDamage), "Notify_PawnPostApplyDamage")]
internal class HediffComp_ReactOnDamage_Notify_PawnPostApplyDamage_Patch_EMP
{
	[HarmonyPrefix]
	private static bool HediffComp_ReactOnDamage_Notify_PawnPostApplyDamage_Prefix(DamageInfo dinfo, float totalDamageDealt, HediffComp_ReactOnDamage __instance)
	{
		float EMPResistance = __instance.Pawn?.GetStatValue(StatDef.Named("NCL_BrainShockResistance")) ?? 0f;
		if (dinfo.Def != DamageDefOf.EMP || !((float)new System.Random().Next(0, 100) < EMPResistance * 100f))
		{
			return true;
		}
		MoteMaker.ThrowText(new Vector3((float)__instance.Pawn.Position.x + 1f, __instance.Pawn.Position.y, (float)__instance.Pawn.Position.z + 1f), text: "Resisted".Translate(), map: __instance.Pawn.Map, color: Color.white);
		return false;
	}
}
