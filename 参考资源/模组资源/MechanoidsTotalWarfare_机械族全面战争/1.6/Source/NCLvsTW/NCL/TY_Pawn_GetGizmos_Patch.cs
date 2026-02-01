using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NCL;

[HarmonyPatch(typeof(Pawn), "GetGizmos")]
public static class TY_Pawn_GetGizmos_Patch
{
	[HarmonyPostfix]
	public static void GetEquippedGizmos(Pawn __instance, ref IEnumerable<Gizmo> __result)
	{
		ThingWithComps primary = __instance.equipment?.Primary;
		if (primary != null)
		{
			Comp_AdvancedAmmo comp = primary.GetComp<Comp_AdvancedAmmo>();
			if (comp != null && __instance.Faction == Faction.OfPlayer)
			{
				__result = __result.Concat(comp.CompGetGizmosExtra());
			}
		}
	}
}
