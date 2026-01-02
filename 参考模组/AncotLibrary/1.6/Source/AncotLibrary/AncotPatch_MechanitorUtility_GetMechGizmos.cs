using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(MechanitorUtility))]
[HarmonyPatch("GetMechGizmos")]
public static class AncotPatch_MechanitorUtility_GetMechGizmos
{
	[HarmonyPrefix]
	public static bool Prefix(Pawn mech, ref IEnumerable<Gizmo> __result)
	{
		if (mech.TryGetComp<CompDrone>() != null)
		{
			__result = Enumerable.Empty<Gizmo>();
			return false;
		}
		return true;
	}
}
