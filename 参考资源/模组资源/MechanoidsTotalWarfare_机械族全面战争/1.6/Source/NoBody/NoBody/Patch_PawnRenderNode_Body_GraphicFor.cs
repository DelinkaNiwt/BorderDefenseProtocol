using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoBody;

[HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
public static class Patch_PawnRenderNode_Body_GraphicFor
{
	private static bool Prefix(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result)
	{
		if (pawn?.def?.defName != "Human")
		{
			return true;
		}
		bool flag = false;
		foreach (Apparel item in pawn.apparel?.WornApparel ?? new List<Apparel>())
		{
			if (item.def == TransparentBodyDefOf.Apparel_NoBody)
			{
				Comp_ForceBodyType comp = item.GetComp<Comp_ForceBodyType>();
				if (comp != null && comp.enableNoBody)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			return true;
		}
		Shader value = Traverse.Create(__instance).Method("ShaderFor", pawn).GetValue<Shader>();
		if (value == null)
		{
			__result = null;
			return false;
		}
		BodyTypeDef bodyTypeDef = pawn.story?.bodyType;
		if (bodyTypeDef != null && !bodyTypeDef.defName.EndsWith("Transparent"))
		{
			string defName = bodyTypeDef.defName + "Transparent";
			BodyTypeDef namedSilentFail = DefDatabase<BodyTypeDef>.GetNamedSilentFail(defName);
			if (namedSilentFail != null && !string.IsNullOrEmpty(namedSilentFail.bodyNakedGraphicPath))
			{
				__result = GraphicDatabase.Get<Graphic_Multi>(namedSilentFail.bodyNakedGraphicPath, value, Vector2.one, __instance.ColorFor(pawn));
				return false;
			}
		}
		return true;
	}
}
