using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
	[HarmonyPatch(typeof(CompOverseerSubject), "get_Overseer")]
	public static class MechControl_Overseer_Patch
	{
		public static void Postfix(CompOverseerSubject __instance, ref Pawn __result)
		{
			if (__result == null)
			{
				Pawn mech = __instance.Parent;
				if (GDUtility.MissionComponent.IsSavingMech(mech))
                {
					__result = mech;
                }
			}
		}
	}

	[HarmonyPatch(typeof(CompOverseerSubject), "get_State")] 
	public static class MechControl_State_Patch
	{
		public static void Postfix(CompOverseerSubject __instance, ref OverseerSubjectState __result)
		{
			Pawn mech = __instance.Parent;
			if (__result != OverseerSubjectState.Overseen && GDUtility.MissionComponent.IsSavingMech(mech))
			{
				__result = OverseerSubjectState.Overseen;
			}
		}
	}

	[HarmonyPatch(typeof(MechanitorUtility), "CanDraftMech")]
	public static class MechanitorUtility_CanControl_Patch
	{
		public static void Postfix(Pawn mech, ref AcceptanceReport __result)
		{
			if (GDUtility.MissionComponent.IsSavingMech(mech))
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(MechanitorUtility), "CanControlMech")]
	public static class MechanitorUtility_NeedMechanitor_Patch
	{
		public static bool Prefix(Pawn mech, ref AcceptanceReport __result)
		{
			if (GDUtility.MissionComponent.IsSavingMech(mech))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn_DraftController), "get_ShowDraftGizmo")]
	public static class MechControl_Draft_Patch
	{
		public static void Postfix(Pawn_DraftController __instance, ref bool __result)
		{
			Pawn mech = __instance.pawn;
			if (__result == false && GDUtility.MissionComponent.IsSavingMech(mech))
			{
				__result = true;
			}
		}
	}

	/*[HarmonyPatch(typeof(Need), "get_CurLevel")]
	public static class MechControl_GetEnergy_Patch
	{
		public static bool Prefix(Pawn ___pawn, ref float __result)
		{
			if (GDUtility.MissionComponent.IsSavingMech(___pawn))
			{
				__result = 100f;
				return false;
			}
			return true;
		}
	}*/

	/*[HarmonyPatch(typeof(PawnRenderNodeWorker), "GetMaterialPropertyBlock")]
	public static class SavingMechRender_Patch
	{
		public static bool Prefix(PawnRenderNodeWorker __instance, PawnRenderNode node, Material material, PawnDrawParms parms, ref MaterialPropertyBlock __result)
		{
			Pawn pawn = parms.pawn;
			if (pawn != null && GDUtility.MissionComponent.IsSavingMech(pawn))
			{
				object[] parameters = { node, parms };
				if ((Graphic)typeof(PawnRenderNodeWorker).GetMethod("GetGraphic", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, parameters) == null)
				{
					__result = null;
					return false;
				}
				MaterialPropertyBlock matPropBlock = node.MatPropBlock;
				if (parms.Statue)
				{
					matPropBlock.SetColor(ShaderPropertyIDs.Color, parms.statueColor.Value);
				}
				else
				{
					matPropBlock.SetColor(ShaderPropertyIDs.Color, parms.tint * material.color);
				}
				if (material.shader == ShaderDatabase.CutoutWithOverlay)
				{
					PawnRenderUtility.SetMatPropBlockOverlay(matPropBlock, Color.white, 0f);
				}
				__result = matPropBlock;
				return false;
			}
			return true;
		}
	}*/
}