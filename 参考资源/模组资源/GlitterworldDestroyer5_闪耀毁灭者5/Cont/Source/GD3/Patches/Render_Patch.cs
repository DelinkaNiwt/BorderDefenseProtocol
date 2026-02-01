using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace GD3
{
	[HarmonyPatch(typeof(Pawn), "DrawAt")]
	public static class HediffPostDraw_Patch
	{
		public static void Postfix(Pawn __instance)
		{
			List<Hediff> list = __instance?.health?.hediffSet?.hediffs;
			if (!list.NullOrEmpty())
			{
				foreach (Hediff hediff in list)
				{
					if (hediff is HediffDrawer drawer)
					{
						drawer.PostDraw();
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(PawnRenderNodeWorker), "OffsetFor")]
	public static class BlackSuitRender_Offset_Patch
	{
		public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
		{
			if (node is PawnRenderNode_Apparel newNode && parms.pawn?.story?.bodyType == BodyTypeDefOf.Hulk)
			{
				ModExtension_DrawOffset ext = newNode.apparel?.def.GetModExtension<ModExtension_DrawOffset>();
				__result += ext?.offset ?? Vector3.zero;
			}
		}
	}

	[HarmonyPatch(typeof(PawnRenderNodeWorker), "ScaleFor")]
	public static class BlackSuitRender_Scale_Patch
	{
		public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
		{
			if (node is PawnRenderNode_Apparel newNode && parms.pawn?.story?.bodyType == BodyTypeDefOf.Hulk)
			{
				ModExtension_DrawOffset ext = newNode.apparel?.def.GetModExtension<ModExtension_DrawOffset>();
				if (ext != null)
                {
					__result.x += ext.extraScale;
					__result.z += ext.extraScale;
				}
			}
		}
	}

	[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
	public static class DrawWeaponGlow_Patch
	{
		public static void Postfix(Thing eq, Vector3 drawLoc, float aimAngle)
		{
			Ext_GlowWeapon ext;
			if ((ext = eq.def.GetModExtension<Ext_GlowWeapon>()) != null)
			{
				DrawGlowAiming(eq, drawLoc, aimAngle, ext);
			}
		}

		public static void DrawGlowAiming(Thing eq, Vector3 drawLoc, float aimAngle, Ext_GlowWeapon ext)
		{
			float num = aimAngle - 90f;
			Mesh mesh;
			if (aimAngle > 20f && aimAngle < 160f)
			{
				mesh = MeshPool.plane10;
				num += eq.def.equippedAngleOffset;
			}
			else if (aimAngle > 200f && aimAngle < 340f)
			{
				mesh = MeshPool.plane10Flip;
				num -= 180f;
				num -= eq.def.equippedAngleOffset;
			}
			else
			{
				mesh = MeshPool.plane10;
				num += eq.def.equippedAngleOffset;
			}

			num %= 360f;
			CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
			if (compEquippable != null)
			{
				EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out Vector3 drawOffset, out float angleOffset, aimAngle);
				drawLoc += drawOffset;
				num += angleOffset;
			}

			Graphic_StackCount graphic_StackCount = ext.graphicData.Graphic as Graphic_StackCount;
			Material material = (graphic_StackCount == null) ? ext.graphicData.Graphic.MatSingleFor(eq) : graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingleFor(eq);
			Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(ext.graphicData.Graphic.drawSize.x, 0f, ext.graphicData.Graphic.drawSize.y), pos: drawLoc, q: Quaternion.AngleAxis(num, Vector3.up));
			Graphics.DrawMesh(mesh, matrix, material, 0);
		}
	}
}
