using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.Prompt;

public static class PromptContextProvider
{
	public static string GetDialogueTypeString(TalkRequest talkRequest, List<Pawn> pawns)
	{
		if (pawns == null || pawns.Count == 0)
		{
			return "";
		}
		Pawn mainPawn = pawns[0];
		string shortName = mainPawn.LabelShort;
		StringBuilder sb = new StringBuilder();
		ContextBuilder.BuildDialogueType(sb, talkRequest, pawns, shortName, mainPawn);
		return sb.ToString();
	}

	public static string GetLocationString(Pawn pawn)
	{
		if (pawn?.Map == null)
		{
			return "";
		}
		string locationStatus = ContextHelper.GetPawnLocationStatus(pawn);
		if (string.IsNullOrEmpty(locationStatus))
		{
			return "";
		}
		int temperature = Mathf.RoundToInt(pawn.Position.GetTemperature(pawn.Map));
		Room room = pawn.GetRoom();
		string roomRole = ((room == null || room.PsychologicallyOutdoors) ? "" : (room.Role?.label ?? ""));
		return string.IsNullOrEmpty(roomRole) ? $"{locationStatus};{temperature}C" : $"{locationStatus};{temperature}C;{roomRole}";
	}

	public static string GetBeautyString(Pawn pawn)
	{
		if (pawn?.Map == null)
		{
			return "";
		}
		List<IntVec3> nearbyCells = ContextHelper.GetNearbyCells(pawn);
		if (nearbyCells.Count == 0)
		{
			return "";
		}
		float beautySum = nearbyCells.Sum((IntVec3 c) => BeautyUtility.CellBeauty(c, pawn.Map));
		return Describer.Beauty(beautySum / (float)nearbyCells.Count);
	}

	public static string GetCleanlinessString(Pawn pawn)
	{
		if (pawn?.Map == null)
		{
			return "";
		}
		Room room = pawn.GetRoom();
		if (room == null || room.PsychologicallyOutdoors)
		{
			return "";
		}
		return Describer.Cleanliness(room.GetStat(RoomStatDefOf.Cleanliness));
	}
}
