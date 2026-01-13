using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Bombardment), "TryDoExplosion")]
public static class Patch_Bombardment_TryDoExplosion
{
	public static bool Prefix(Bombardment __instance, Bombardment.BombardmentProjectile proj)
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
			if (comp_AbsoluteTerrorField.CheckBombardmentIntercept(__instance, proj))
			{
				return false;
			}
		}
		return true;
	}
}
