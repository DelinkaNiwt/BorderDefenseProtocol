using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(InfestationCellFinder), "GetScoreAt")]
public static class Patch_InfestationCellFinder_GetScoreAt
{
	public static void Postfix(IntVec3 cell, Map map, ref float __result)
	{
		if (__result <= 0f)
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
			if (comp_AbsoluteTerrorField.Active && cell.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
			{
				__result = 0f;
				break;
			}
		}
	}
}
