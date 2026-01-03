using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(MechanitorUtility))]
[HarmonyPatch("ShouldBeMechanitor")]
public static class Milian_MechanitorUtility_ShouldBeMechanitor_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn, ref bool __result)
	{
		if (!__result && pawn.health.hediffSet.HasHediff(MiliraDefOf.Milira_MilianHomeTerminal) && ModsConfig.BiotechActive && pawn.Faction.IsPlayerSafe())
		{
			__result = true;
		}
	}
}
