using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(JoyUtility), "TryGainRecRoomThought")]
public static class JoyUtility_TryGainRecRoomThought_Patch
{
	public static void Prefix(Pawn pawn)
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = pawn;
	}

	public static void Postfix()
	{
		RoomStatDef_GetScoreStageIndex_Patch.forPawn = null;
	}
}
