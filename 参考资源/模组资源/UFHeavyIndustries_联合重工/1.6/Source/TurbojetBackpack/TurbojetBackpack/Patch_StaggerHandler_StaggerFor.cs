using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(StaggerHandler), "StaggerFor")]
public static class Patch_StaggerHandler_StaggerFor
{
	public static bool Prefix(StaggerHandler __instance, Pawn ___parent, ref bool __result)
	{
		if (___parent == null || ___parent.apparel == null)
		{
			return true;
		}
		List<Apparel> wornApparel = ___parent.apparel.WornApparel;
		for (int i = 0; i < wornApparel.Count; i++)
		{
			CompTurbojetShield comp = wornApparel[i].GetComp<CompTurbojetShield>();
			if (comp != null && comp.State == ShieldState.Active)
			{
				__result = false;
				return false;
			}
		}
		return true;
	}
}
