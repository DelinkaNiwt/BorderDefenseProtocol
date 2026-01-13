using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Verb_ShootBeam), "HitCell")]
public static class Patch_Verb_ShootBeam_HitCell
{
	public static bool Prefix(Verb_ShootBeam __instance, IntVec3 cell, IntVec3 sourceCell)
	{
		if (__instance.Caster == null || __instance.Caster.Map == null)
		{
			return true;
		}
		List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(__instance.Caster.Map).activeFields;
		if (activeFields == null || activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
			if (comp_AbsoluteTerrorField.CheckVerbShootBeamIntercept(__instance, cell, sourceCell))
			{
				return false;
			}
		}
		return true;
	}
}
