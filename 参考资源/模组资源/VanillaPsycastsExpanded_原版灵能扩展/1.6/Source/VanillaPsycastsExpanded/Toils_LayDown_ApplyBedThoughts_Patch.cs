using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Toils_LayDown), "ApplyBedThoughts")]
public static class Toils_LayDown_ApplyBedThoughts_Patch
{
	public static void Prefix(Pawn actor)
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = actor;
	}

	public static void Postfix()
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = null;
	}
}
