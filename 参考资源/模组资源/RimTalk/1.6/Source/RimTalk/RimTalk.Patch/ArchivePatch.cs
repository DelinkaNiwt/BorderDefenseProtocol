using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.Source.Data;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(Archive), "Add")]
public static class ArchivePatch
{
	public static void Prefix(IArchivable archivable)
	{
		RimTalkSettings settings = Settings.Get();
		string typeName = archivable.GetType().FullName;
		if (!settings.EnabledArchivableTypes.ContainsKey(typeName) || !settings.EnabledArchivableTypes[typeName])
		{
			return;
		}
		var (prompt, talkType) = GeneratePrompt(archivable);
		var (eventMap, nearbyColonists) = FindLocationAndColonists(archivable);
		if (nearbyColonists.Any())
		{
			foreach (Pawn pawn in nearbyColonists)
			{
				Cache.Get(pawn)?.AddTalkRequest(prompt, null, talkType);
			}
			return;
		}
		TalkRequestPool.Add(prompt, null, null, eventMap?.uniqueID ?? 0);
	}

	private static (string prompt, TalkType talkType) GeneratePrompt(IArchivable archivable)
	{
		TalkType talkType = TalkType.Event;
		string prompt;
		if (archivable is ChoiceLetter { quest: not null } choiceLetter)
		{
			if (choiceLetter.quest.State == QuestState.NotYetAccepted)
			{
				talkType = TalkType.QuestOffer;
				prompt = "(Talk if you want to accept quest)\n[" + choiceLetter.quest.description.ToString().StripTags() + "]";
			}
			else
			{
				talkType = TalkType.QuestEnd;
				prompt = "(Talk about quest result)\n[" + archivable.ArchivedTooltip.StripTags() + "]";
			}
		}
		else if (archivable is Letter letter && !(letter is ChoiceLetter))
		{
			string label = archivable.ArchivedLabel ?? string.Empty;
			string tip = archivable.ArchivedTooltip ?? string.Empty;
			if (ContainsQuestReference(label, tip))
			{
				talkType = TalkType.QuestEnd;
				prompt = "(Talk about quest result)\n[" + tip.StripTags() + "]";
			}
			else
			{
				prompt = "(Talk about incident)\n[" + tip.StripTags() + "]";
			}
		}
		else
		{
			prompt = "(Talk about incident)\n[" + archivable.ArchivedTooltip.StripTags() + "]";
		}
		return (prompt: prompt, talkType: talkType);
	}

	private static bool ContainsQuestReference(string label, string tip)
	{
		return label.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0 || tip.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static (Map eventMap, List<Pawn> nearbyColonists) FindLocationAndColonists(IArchivable archivable)
	{
		Map eventMap = null;
		List<Pawn> nearbyColonists = new List<Pawn>();
		LookTargets lookTargets = archivable.LookTargets;
		if (lookTargets == null || !lookTargets.Any)
		{
			return (eventMap: null, nearbyColonists: nearbyColonists);
		}
		eventMap = archivable.LookTargets.PrimaryTarget.Map ?? archivable.LookTargets.targets.Select((GlobalTargetInfo t) => t.Map).FirstOrDefault((Map m) => m != null);
		if (eventMap != null)
		{
			nearbyColonists = eventMap.mapPawns.AllPawnsSpawned.Where((Pawn pawn) => pawn.IsFreeNonSlaveColonist && !pawn.IsQuestLodger() && (Cache.Get(pawn)?.CanDisplayTalk() ?? false)).ToList();
		}
		return (eventMap: eventMap, nearbyColonists: nearbyColonists);
	}
}
