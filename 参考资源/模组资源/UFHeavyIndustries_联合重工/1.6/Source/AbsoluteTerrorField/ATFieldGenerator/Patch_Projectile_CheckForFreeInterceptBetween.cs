using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Projectile), "CheckForFreeInterceptBetween")]
public static class Patch_Projectile_CheckForFreeInterceptBetween
{
	public static bool Prefix(Projectile __instance, Vector3 lastExactPos, Vector3 newExactPos, ref bool __result)
	{
		Map map = __instance.Map;
		if (map == null)
		{
			return true;
		}
		List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(map).activeFields;
		if (activeFields == null || activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
			if (!comp_AbsoluteTerrorField.CheckIntercept(__instance, lastExactPos, newExactPos))
			{
				continue;
			}
			__result = true;
			if (comp_AbsoluteTerrorField.reflectMode)
			{
				Vector3 exactPosition = __instance.ExactPosition;
				IntVec3 cell;
				if (__instance.Launcher != null && __instance.Launcher.Map == __instance.Map && __instance.Launcher.Position.IsValid)
				{
					IntVec3 position = __instance.Launcher.Position;
					float reflectMissRadius = comp_AbsoluteTerrorField.Props.reflectMissRadius;
					IntVec3 intVec = Vector3Utility.RandomHorizontalOffset(reflectMissRadius).ToIntVec3();
					cell = (position + intVec).ClampInsideMap(__instance.Map);
				}
				else
				{
					Vector3 vector = __instance.ExactRotation * Vector3.forward;
					float reflectScatterFactor = comp_AbsoluteTerrorField.Props.reflectScatterFactor;
					Vector3 normalized = (-vector + Vector3Utility.RandomHorizontalOffset(1f) * reflectScatterFactor).normalized;
					Vector3 vect = exactPosition + normalized * 60f;
					cell = vect.ToIntVec3();
				}
				Projectile projectile = (Projectile)GenSpawn.Spawn(__instance.def, exactPosition.ToIntVec3(), __instance.Map);
				Thing equipment = null;
				FieldInfo fieldInfo = AccessTools.Field(typeof(Projectile), "equipment");
				if (fieldInfo != null)
				{
					equipment = (Thing)fieldInfo.GetValue(__instance);
				}
				projectile.Launch(comp_AbsoluteTerrorField.parent, exactPosition, new LocalTargetInfo(cell), new LocalTargetInfo(cell), ProjectileHitFlags.All, preventFriendlyFire: false, equipment);
			}
			__instance.Destroy();
			return false;
		}
		return true;
	}
}
