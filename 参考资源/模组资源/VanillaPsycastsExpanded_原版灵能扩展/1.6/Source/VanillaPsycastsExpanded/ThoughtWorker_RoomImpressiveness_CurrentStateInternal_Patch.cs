using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(ThoughtWorker_RoomImpressiveness), "CurrentStateInternal")]
public static class ThoughtWorker_RoomImpressiveness_CurrentStateInternal_Patch
{
	public static void Prefix(Pawn p)
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = p;
	}

	public static void Postfix()
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = null;
	}
}
