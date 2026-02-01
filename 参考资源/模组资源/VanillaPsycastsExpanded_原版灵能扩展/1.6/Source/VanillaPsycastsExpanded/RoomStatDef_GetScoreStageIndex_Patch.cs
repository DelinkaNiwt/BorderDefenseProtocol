using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(RoomStatDef), "GetScoreStageIndex")]
public static class RoomStatDef_GetScoreStageIndex_Patch
{
	public static Pawn forPawn;

	public static void Postfix(RoomStatDef __instance, ref int __result)
	{
		if (forPawn != null && forPawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_Hallucination) != null)
		{
			__result = __instance.scoreStages.Count - 1;
		}
		forPawn = null;
	}
}
