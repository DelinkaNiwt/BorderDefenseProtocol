using System;
using System.Linq;
using Bubbles;
using Bubbles.Core;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.Patches;
using RimTalk.Service;
using RimTalk.Source.Data;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(Bubbler), "Add")]
public static class Bubbler_Add
{
	private static bool _originalDraftedValue;

	public static bool Prefix(LogEntry entry)
	{
		RimTalkSettings settings = Settings.Get();
		Pawn initiator = (Pawn)entry.GetConcerns().First();
		Pawn recipient = GetRecipient(entry);
		string prompt = entry.ToGameStringFromPOV(initiator).StripTags();
		if (IsRimTalkInteraction(entry))
		{
			if (settings.DisplayTalkWhenDrafted)
			{
				try
				{
					_originalDraftedValue = Settings.DoDrafted.Value;
					Settings.DoDrafted.Value = true;
				}
				catch (Exception ex)
				{
					Logger.Warning("Failed to override bubble drafted setting: " + ex.Message);
				}
			}
			return true;
		}
		if (!settings.IsEnabled || !settings.ProcessNonRimTalkInteractions)
		{
			return true;
		}
		InteractionDef interactionDef = GetInteractionDef(entry);
		if (interactionDef == null)
		{
			return true;
		}
		bool isChitchat = interactionDef == InteractionDefOf.Chitchat || interactionDef == InteractionDefOf.DeepTalk;
		if (isChitchat && (initiator.IsInDanger() || initiator.GetHostilePawnNearBy() != null || !PawnSelector.GetNearByTalkablePawns(initiator).Contains(recipient)))
		{
			return false;
		}
		PawnState pawnState = Cache.Get(initiator);
		if (pawnState == null || (isChitchat && pawnState.TalkRequests.Count > 0))
		{
			return false;
		}
		prompt = prompt + " (" + interactionDef.label + ")";
		pawnState.AddTalkRequest(prompt, recipient, TalkType.Chitchat);
		return false;
	}

	public static void Postfix()
	{
		if (Settings.Get().DisplayTalkWhenDrafted)
		{
			try
			{
				Settings.DoDrafted.Value = _originalDraftedValue;
			}
			catch (Exception ex)
			{
				Logger.Warning("Failed to restore bubble drafted setting: " + ex.Message);
			}
		}
	}

	private static Pawn GetRecipient(LogEntry entry)
	{
		return entry.GetConcerns().Skip(1).OfType<Pawn>()
			.FirstOrDefault();
	}

	private static bool IsRimTalkInteraction(LogEntry entry)
	{
		return entry is PlayLogEntry_RimTalkInteraction || (entry is PlayLogEntry_Interaction interaction && InteractionTextPatch.IsRimTalkInteraction(interaction));
	}

	private static InteractionDef GetInteractionDef(LogEntry entry)
	{
		return AccessTools.Field(entry.GetType(), "intDef")?.GetValue(entry) as InteractionDef;
	}
}
