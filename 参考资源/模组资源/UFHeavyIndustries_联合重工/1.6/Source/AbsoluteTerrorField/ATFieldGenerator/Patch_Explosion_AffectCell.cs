using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Explosion), "AffectCell")]
public static class Patch_Explosion_AffectCell
{
	public static bool Prefix(Explosion __instance, IntVec3 c)
	{
		if (__instance.Map == null)
		{
			return true;
		}
		List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(__instance.Map).activeFields;
		if (activeFields == null || activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
			if (comp_AbsoluteTerrorField.CheckExplosionAffectCellIntercept(c))
			{
				return false;
			}
		}
		return true;
	}
}
