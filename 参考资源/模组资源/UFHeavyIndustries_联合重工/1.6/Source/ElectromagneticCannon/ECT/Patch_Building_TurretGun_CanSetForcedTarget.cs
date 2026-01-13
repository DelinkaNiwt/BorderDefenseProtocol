using HarmonyLib;
using RimWorld;

namespace ECT;

[HarmonyPatch(typeof(Building_TurretGun), "get_CanSetForcedTarget")]
public static class Patch_Building_TurretGun_CanSetForcedTarget
{
	public static void Postfix(Building_TurretGun __instance, ref bool __result)
	{
		if (!__result && __instance.GetComp<CompManualAimMode>() != null && __instance.Faction == Faction.OfPlayer)
		{
			__result = true;
		}
	}
}
