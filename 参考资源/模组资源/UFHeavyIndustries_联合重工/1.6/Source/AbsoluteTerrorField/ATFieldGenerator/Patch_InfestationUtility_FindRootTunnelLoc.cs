using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(InfestationUtility), "FindRootTunnelLoc")]
public static class Patch_InfestationUtility_FindRootTunnelLoc
{
	public static void Postfix(Map map, ref IntVec3 __result)
	{
		if (!__result.IsValid)
		{
			return;
		}
		ATFieldManager aTFieldManager = ATFieldManager.Get(map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return;
		}
		for (int i = 0; i < aTFieldManager.activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = aTFieldManager.activeFields[i];
			if (!comp_AbsoluteTerrorField.Active || !__result.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
			{
				continue;
			}
			Vector3 vector = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
			Vector3 vector2 = __result.ToVector3Shifted();
			Vector3 vector3 = (vector2 - vector).normalized;
			if (vector3 == Vector3.zero)
			{
				vector3 = Vector3.right;
			}
			Vector3 vect = vector + vector3 * (comp_AbsoluteTerrorField.radius + 5f);
			IntVec3 intVec = vect.ToIntVec3().ClampInsideMap(map);
			bool flag = false;
			for (int j = 0; j < aTFieldManager.activeFields.Count; j++)
			{
				if (aTFieldManager.activeFields[j].Active && intVec.InHorDistOf(aTFieldManager.activeFields[j].parent.Position, aTFieldManager.activeFields[j].radius))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				__result = intVec;
			}
			else
			{
				__result = IntVec3.Invalid;
			}
			break;
		}
	}
}
