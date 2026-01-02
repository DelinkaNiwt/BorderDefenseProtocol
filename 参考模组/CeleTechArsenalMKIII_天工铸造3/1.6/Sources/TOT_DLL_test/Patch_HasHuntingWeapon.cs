using HarmonyLib;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
internal static class Patch_HasHuntingWeapon
{
	private static void Postfix(Pawn p, ref bool __result)
	{
		if (!__result)
		{
			__result = p.equipment.Primary != null && p.equipment.PrimaryEq.PrimaryVerb != null && p.equipment.PrimaryEq.PrimaryVerb is Verb_ShootSwitchFire;
		}
	}
}
