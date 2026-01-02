using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(RestUtility))]
[HarmonyPatch("IsSelfShutdown")]
public static class AncotPatch_IsSelfShutdown_Drone
{
	[HarmonyPostfix]
	public static void Postfix(ref bool __result, Pawn p)
	{
		if (__result)
		{
			CompDrone compDrone = p.TryGetComp<CompDrone>();
			if (compDrone != null)
			{
				__result = compDrone.depleted;
			}
		}
	}
}
