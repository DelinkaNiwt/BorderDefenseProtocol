using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(FloatMenuOptionProvider_Mechanitor))]
[HarmonyPatch("GetOptionsFor")]
public static class AncotPatch_CanControlDrone
{
	[HarmonyPrefix]
	public static bool Prefix(ref IEnumerable<FloatMenuOption> __result, Pawn clickedPawn)
	{
		if (clickedPawn.TryGetComp<CompDrone>() != null)
		{
			__result = Enumerable.Empty<FloatMenuOption>();
			return false;
		}
		return true;
	}
}
