using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalk.Patches;

[HarmonyPatch(typeof(GameDataSaveLoader), "SaveGame")]
public static class SaveGamePatch
{
	[HarmonyPrefix]
	public static void PreSaveGame()
	{
		try
		{
			List<LogEntry> entries = Find.PlayLog?.AllEntries;
			if (entries == null)
			{
				return;
			}
			RimTalkWorldComponent worldComp = Find.World.GetComponent<RimTalkWorldComponent>();
			if (worldComp == null)
			{
				Logger.Error("RimTalkWorldComponent not found");
				return;
			}
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i] is PlayLogEntry_RimTalkInteraction rimTalkEntry)
				{
					PlayLogEntry_Interaction newEntry = new PlayLogEntry_Interaction(InteractionDefOf.Chitchat, rimTalkEntry.Initiator, rimTalkEntry.Recipient, rimTalkEntry.ExtraSentencePacks ?? new List<RulePackDef>());
					typeof(LogEntry).GetField("ticksAbs", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(newEntry, rimTalkEntry.TicksAbs);
					worldComp.SetTextFor(newEntry, rimTalkEntry.CachedString);
					entries[i] = newEntry;
				}
			}
		}
		catch (Exception arg)
		{
			Logger.Error($"Error converting RimTalk interactions: {arg}");
		}
	}
}
