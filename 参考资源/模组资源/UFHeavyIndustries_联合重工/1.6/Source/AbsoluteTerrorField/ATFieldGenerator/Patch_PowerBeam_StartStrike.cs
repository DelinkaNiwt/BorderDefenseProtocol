using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(PowerBeam), "StartStrike")]
public static class Patch_PowerBeam_StartStrike
{
	public static bool Prefix(PowerBeam __instance)
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
			if (comp_AbsoluteTerrorField.Active && __instance.Position.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
			{
				__instance.Destroy();
				FleckMaker.ThrowMicroSparks(__instance.Position.ToVector3Shifted(), __instance.Map);
				return false;
			}
		}
		return true;
	}
}
