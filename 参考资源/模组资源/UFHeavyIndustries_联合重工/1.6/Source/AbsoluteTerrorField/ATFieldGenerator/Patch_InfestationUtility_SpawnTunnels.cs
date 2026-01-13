using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(InfestationUtility), "SpawnTunnels")]
public static class Patch_InfestationUtility_SpawnTunnels
{
	public static bool Prefix(ref int hiveCount, Map map, bool spawnAnywhereIfNoGoodCell, bool ignoreRoofedRequirement, string questTag, ref IntVec3? overrideLoc, float? insectsPoints, ref Thing __result)
	{
		if (!overrideLoc.HasValue)
		{
			return true;
		}
		IntVec3 value = overrideLoc.Value;
		ATFieldManager aTFieldManager = ATFieldManager.Get(map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return true;
		}
		bool flag = false;
		Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = null;
		for (int i = 0; i < aTFieldManager.activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField2 = aTFieldManager.activeFields[i];
			if (comp_AbsoluteTerrorField2.Active && value.InHorDistOf(comp_AbsoluteTerrorField2.parent.Position, comp_AbsoluteTerrorField2.radius))
			{
				flag = true;
				comp_AbsoluteTerrorField = comp_AbsoluteTerrorField2;
				break;
			}
		}
		if (flag)
		{
			Vector3 vector = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
			Vector3 vector2 = (value.ToVector3Shifted() - vector).normalized;
			if (vector2 == Vector3.zero)
			{
				vector2 = Vector3.right;
			}
			IntVec3 value2 = (vector + vector2 * (comp_AbsoluteTerrorField.radius + 5f)).ToIntVec3().ClampInsideMap(map);
			bool flag2 = true;
			for (int j = 0; j < aTFieldManager.activeFields.Count; j++)
			{
				if (aTFieldManager.activeFields[j].Active && value2.InHorDistOf(aTFieldManager.activeFields[j].parent.Position, aTFieldManager.activeFields[j].radius))
				{
					flag2 = false;
					break;
				}
			}
			if (!flag2)
			{
				FleckMaker.ThrowMicroSparks(value.ToVector3Shifted(), map);
				__result = null;
				return false;
			}
			overrideLoc = value2;
		}
		return true;
	}
}
