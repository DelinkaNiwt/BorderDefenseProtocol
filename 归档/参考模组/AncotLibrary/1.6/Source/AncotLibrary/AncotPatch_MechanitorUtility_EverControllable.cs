using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(MechanitorUtility), "EverControllable")]
public static class AncotPatch_MechanitorUtility_EverControllable
{
	[HarmonyPostfix]
	public static void Postfix(Pawn mech, ref bool __result)
	{
		if (!__result && mech.TryGetComp<CompDrone>() != null)
		{
			__result = true;
		}
	}
}
