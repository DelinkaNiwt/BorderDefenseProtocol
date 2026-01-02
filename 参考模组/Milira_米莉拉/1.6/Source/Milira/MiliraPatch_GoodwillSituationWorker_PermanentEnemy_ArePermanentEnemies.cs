using HarmonyLib;
using RimWorld;

namespace Milira;

[HarmonyPatch(typeof(GoodwillSituationWorker_PermanentEnemy))]
[HarmonyPatch("ArePermanentEnemies")]
public static class MiliraPatch_GoodwillSituationWorker_PermanentEnemy_ArePermanentEnemies
{
	public static void Postfix(ref bool __result, Faction a, Faction b)
	{
		if (a.def == MiliraDefOf.Milira_Faction && b.IsPlayer)
		{
			__result = MiliraGameComponent_OverallControl.OverallControl.turnToFriend_Pre;
		}
	}
}
