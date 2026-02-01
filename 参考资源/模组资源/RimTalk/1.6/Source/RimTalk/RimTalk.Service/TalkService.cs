using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Source.Data;
using RimTalk.UI;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalk.Service;

public static class TalkService
{
	public static bool GenerateTalk(TalkRequest talkRequest)
	{
		RimTalkSettings settings = Settings.Get();
		if (!settings.IsEnabled || !CommonUtil.ShouldAiBeActiveOnSpeed())
		{
			return false;
		}
		if (settings.GetActiveConfig() == null)
		{
			return false;
		}
		if (AIService.IsBusy())
		{
			return false;
		}
		PawnState pawn1 = Cache.Get(talkRequest.Initiator);
		if (!talkRequest.TalkType.IsFromUser() && (pawn1 == null || !pawn1.CanGenerateTalk()))
		{
			return false;
		}
		if (!settings.AllowSimultaneousConversations && AnyPawnHasPendingResponses())
		{
			return false;
		}
		PawnState pawn2 = ((talkRequest.Recipient != null) ? Cache.Get(talkRequest.Recipient) : null);
		if (pawn2 == null || talkRequest.Recipient?.Name == null || !pawn2.CanDisplayTalk())
		{
			talkRequest.Recipient = null;
		}
		List<Pawn> nearbyPawns = PawnSelector.GetAllNearByPawns(talkRequest.Initiator);
		if (talkRequest.Recipient.IsPlayer())
		{
			nearbyPawns.Insert(0, talkRequest.Recipient);
		}
		var (status, isInDanger) = talkRequest.Initiator.GetPawnStatusFull(nearbyPawns);
		if (!talkRequest.TalkType.IsFromUser() && status == pawn1.LastStatus && pawn1.RejectCount < 2)
		{
			pawn1.RejectCount++;
			return false;
		}
		if (!talkRequest.TalkType.IsFromUser() && isInDanger)
		{
			talkRequest.TalkType = TalkType.Urgent;
		}
		pawn1.RejectCount = 0;
		pawn1.LastStatus = status;
		List<Pawn> pawns = new List<Pawn> { talkRequest.Initiator, talkRequest.Recipient }.Where((Pawn p) => p != null).Concat(nearbyPawns.Where(delegate(Pawn p)
		{
			PawnState pawnState = Cache.Get(p);
			return pawnState.CanDisplayTalk() && pawnState.TalkResponses.Empty();
		})).Distinct()
			.Take(settings.Context.MaxPawnContextCount)
			.ToList();
		if (pawns.Count == 1)
		{
			talkRequest.IsMonologue = true;
		}
		if (!settings.AllowMonologue && talkRequest.IsMonologue && !talkRequest.TalkType.IsFromUser())
		{
			return false;
		}
		talkRequest.PromptMessages = PromptManager.Instance.BuildMessages(talkRequest, pawns, status);
		Task.Run(() => GenerateAndProcessTalkAsync(talkRequest));
		pawn1.MarkRequestSpoken(talkRequest);
		return true;
	}

	private static async Task GenerateAndProcessTalkAsync(TalkRequest talkRequest)
	{
		Pawn initiator = talkRequest.Initiator;
		try
		{
			Cache.Get(initiator).IsGeneratingTalk = true;
			List<TalkResponse> receivedResponses = new List<TalkResponse>();
			await AIService.ChatStreaming(talkRequest, delegate(TalkResponse talkResponse)
			{
				Logger.Debug($"Streamed: {talkResponse}");
				PawnState byName = Cache.GetByName(talkResponse.Name);
				talkResponse.Name = byName.Pawn.LabelShort;
				if (receivedResponses.Any())
				{
					talkResponse.ParentTalkId = receivedResponses.Last().Id;
				}
				receivedResponses.Add(talkResponse);
				byName.TalkResponses.Add(talkResponse);
			});
			AddResponsesToHistory(receivedResponses, talkRequest.Prompt);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Error(ex2.StackTrace);
		}
		finally
		{
			Cache.Get(initiator).IsGeneratingTalk = false;
		}
	}

	private static void AddResponsesToHistory(List<TalkResponse> responses, string prompt)
	{
		if (!responses.Any())
		{
			return;
		}
		string serializedResponses = JsonUtil.SerializeToJson(responses);
		IEnumerable<Pawn> uniquePawns = (from r in responses
			select Cache.GetByName(r.Name)?.Pawn into p
			where p != null
			select p).Distinct();
		foreach (Pawn pawn in uniquePawns)
		{
			TalkHistory.AddMessageHistory(pawn, prompt, serializedResponses);
		}
	}

	public static void DisplayTalk()
	{
		foreach (Pawn pawn in Cache.Keys)
		{
			PawnState pawnState = Cache.Get(pawn);
			if (pawnState == null || pawnState.TalkResponses.Empty())
			{
				continue;
			}
			TalkResponse talk = pawnState.TalkResponses.First();
			if (talk == null)
			{
				pawnState.TalkResponses.RemoveAt(0);
				continue;
			}
			if (TalkHistory.IsTalkIgnored(talk.ParentTalkId) || !pawnState.CanDisplayTalk())
			{
				pawnState.IgnoreTalkResponse();
				continue;
			}
			int replyInterval = 4;
			if (pawn.IsInDanger())
			{
				replyInterval = 2;
				pawnState.IgnoreAllTalkResponses(new List<TalkType>(2)
				{
					TalkType.Urgent,
					TalkType.User
				});
			}
			int parentTalkTick = TalkHistory.GetSpokenTick(talk.ParentTalkId);
			if (parentTalkTick == -1 || !CommonUtil.HasPassed(parentTalkTick, replyInterval))
			{
				continue;
			}
			CreateInteraction(pawn, talk);
			break;
		}
	}

	public static string GetTalk(Pawn pawn)
	{
		PawnState pawnState = Cache.Get(pawn);
		if (pawnState == null)
		{
			return null;
		}
		TalkResponse talkResponse = ConsumeTalk(pawnState);
		pawnState.LastTalkTick = GenTicks.TicksGame;
		return talkResponse.Text;
	}

	public static void GenerateTalkDebug(TalkRequest talkRequest)
	{
		Task.Run(() => GenerateAndProcessTalkAsync(talkRequest));
	}

	private static TalkResponse ConsumeTalk(PawnState pawnState)
	{
		if (pawnState.TalkResponses.Empty())
		{
			return new TalkResponse(TalkType.Other, null, "");
		}
		TalkResponse talkResponse = pawnState.TalkResponses.First();
		pawnState.TalkResponses.Remove(talkResponse);
		TalkHistory.AddSpoken(talkResponse.Id);
		ApiLog apiLog = ApiHistory.GetApiLog(talkResponse.Id);
		if (apiLog != null)
		{
			apiLog.SpokenTick = GenTicks.TicksGame;
		}
		Overlay.NotifyLogUpdated();
		return talkResponse;
	}

	private static void CreateInteraction(Pawn pawn, TalkResponse talk)
	{
		InteractionDef intDef = DefDatabase<InteractionDef>.GetNamed("RimTalkInteraction");
		Pawn recipient = talk.GetTarget() ?? pawn;
		PlayLogEntry_RimTalkInteraction playLogEntryInteraction = new PlayLogEntry_RimTalkInteraction(intDef, pawn, recipient, null);
		Find.PlayLog.Add(playLogEntryInteraction);
		if (!Settings.Get().ApplyMoodAndSocialEffects || pawn == recipient)
		{
			return;
		}
		InteractionType interactionType = talk.GetInteractionType();
		ThoughtDef memory = interactionType.GetThoughtDef();
		if (memory != null)
		{
			recipient.needs?.mood?.thoughts?.memories?.TryGainMemory(memory, pawn);
			if (interactionType == InteractionType.Chat)
			{
				pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(memory, recipient);
			}
		}
	}

	private static bool AnyPawnHasPendingResponses()
	{
		return Cache.GetAll().Any((PawnState pawnState) => pawnState.TalkResponses.Count > 0);
	}
}
