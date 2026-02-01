using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RimTalk.Data;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.UI;

public static class UIUtil
{
	public static void DrawClickablePawnName(Rect rect, string pawnName, Pawn pawn = null)
	{
		if (pawn != null)
		{
			Color originalColor = GUI.color;
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = (pawn.IsPlayer() ? new Color(1f, 0.75f, 0.8f) : (pawn.Dead ? Color.gray : PawnNameColorUtility.PawnNameColorOf(pawn)));
			Widgets.Label(rect, "[" + pawnName + "]");
			if (Widgets.ButtonInvisible(rect))
			{
				if (pawn.Dead && pawn.Corpse != null && pawn.Corpse.Spawned)
				{
					CameraJumper.TryJump(pawn.Corpse);
				}
				else if (!pawn.Dead && pawn.Spawned)
				{
					CameraJumper.TryJump(pawn);
				}
			}
			GUI.color = originalColor;
		}
		else
		{
			Widgets.Label(rect, "[" + pawnName + "]");
		}
	}

	public static void ExportLogs(List<ApiLog> apiLogs)
	{
		if (apiLogs == null || !apiLogs.Any())
		{
			Messages.Message("No conversations to export.", MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		try
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Timestamp,Pawn,Response,Type,Tokens,ElapsedMs,Prompt,Contexts");
			foreach (ApiLog log in apiLogs)
			{
				sb.AppendLine($"\"{log.Timestamp}\",\"{log.Name}\",\"{log.Response}\",\"{log.InteractionType}\",{log.Payload?.TokenCount},{log.ElapsedMs},\"{log.TalkRequest.Prompt}\",\"{log.TalkRequest.Context}\"");
			}
			string fileName = $"RimTalk_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
			File.WriteAllText(path, sb.ToString());
			Messages.Message("Exported to: " + path, MessageTypeDefOf.TaskCompletion, historical: false);
		}
		catch (Exception ex)
		{
			global::RimTalk.Util.Logger.Error("Failed to export logs: " + ex.Message);
			Messages.Message("Export failed. Check logs.", MessageTypeDefOf.NegativeEvent, historical: false);
		}
	}
}
