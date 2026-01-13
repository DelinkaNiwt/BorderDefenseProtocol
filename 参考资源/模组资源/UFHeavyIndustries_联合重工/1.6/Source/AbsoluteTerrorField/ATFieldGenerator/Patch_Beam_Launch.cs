using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Beam), "Launch")]
public static class Patch_Beam_Launch
{
	public static bool Prefix(Beam __instance, Thing launcher, LocalTargetInfo usedTarget)
	{
		if (launcher == null || launcher.Map == null)
		{
			return true;
		}
		List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(launcher.Map).activeFields;
		if (activeFields == null || activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
			if (comp_AbsoluteTerrorField.CheckBeamIntercept(__instance, launcher, usedTarget))
			{
				return false;
			}
		}
		return true;
	}
}
