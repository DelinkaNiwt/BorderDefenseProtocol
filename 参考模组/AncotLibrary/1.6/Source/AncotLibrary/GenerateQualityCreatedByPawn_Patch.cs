using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new Type[]
{
	typeof(Pawn),
	typeof(SkillDef),
	typeof(bool)
})]
public static class GenerateQualityCreatedByPawn_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ref QualityCategory __result, Pawn pawn)
	{
		if (__result != QualityCategory.Legendary)
		{
			float statValue = pawn.GetStatValue(AncotDefOf.Ancot_QualityOffset, applyPostProcess: true, 0);
			int num = (int)statValue;
			QualityCategory qualityCategory = (QualityCategory)Mathf.Min((int)(__result + (byte)num), 6);
			__result = qualityCategory;
		}
	}
}
