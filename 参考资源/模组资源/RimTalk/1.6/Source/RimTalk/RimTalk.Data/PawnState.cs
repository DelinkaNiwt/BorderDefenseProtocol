using System.Collections.Generic;
using RimTalk.Source.Data;
using RimTalk.Util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTalk.Data;

public class PawnState(Pawn pawn)
{
	public readonly Pawn Pawn = pawn;

	public readonly List<TalkResponse> TalkResponses = new List<TalkResponse>();

	public readonly LinkedList<TalkRequest> TalkRequests = new LinkedList<TalkRequest>();

	public string Context { get; set; }

	public int LastTalkTick { get; set; } = 0;

	public string LastStatus { get; set; } = "";

	public int RejectCount { get; set; }

	public bool IsGeneratingTalk { get; set; }

	public HashSet<Hediff> Hediffs { get; set; } = pawn.GetHediffs();

	public string Personality => PersonaService.GetPersonality(pawn);

	public double TalkInitiationWeight => PersonaService.GetTalkInitiationWeight(pawn);

	public void AddTalkRequest(string prompt, Pawn recipient = null, TalkType talkType = TalkType.Other)
	{
		if (talkType == TalkType.Urgent)
		{
			LinkedListNode<TalkRequest> currentNode = TalkRequests.First;
			while (currentNode != null)
			{
				LinkedListNode<TalkRequest> nextNode = currentNode.Next;
				TalkRequest request = currentNode.Value;
				if (!request.TalkType.IsFromUser())
				{
					TalkRequestPool.AddToHistory(request, RequestStatus.Expired);
					TalkRequests.Remove(currentNode);
				}
				currentNode = nextNode;
			}
		}
		TalkRequest newRequest = new TalkRequest(prompt, pawn, recipient, talkType)
		{
			Status = RequestStatus.Pending
		};
		if (talkType.IsFromUser())
		{
			TalkRequests.AddFirst(newRequest);
			IgnoreAllTalkResponses();
			Cache.Get(recipient)?.IgnoreAllTalkResponses();
			UserRequestPool.Add(pawn);
		}
		else if ((uint)(talkType - 4) <= 1u)
		{
			TalkRequests.AddFirst(newRequest);
		}
		else
		{
			TalkRequests.AddLast(newRequest);
		}
	}

	public TalkRequest GetNextTalkRequest()
	{
		LinkedListNode<TalkRequest> node = TalkRequests.First;
		while (node != null)
		{
			TalkRequest request = node.Value;
			LinkedListNode<TalkRequest> next = node.Next;
			if (!request.IsExpired())
			{
				return request;
			}
			TalkRequestPool.AddToHistory(request, RequestStatus.Expired);
			TalkRequests.Remove(node);
			node = next;
		}
		return null;
	}

	public void MarkRequestSpoken(TalkRequest request)
	{
		TalkRequestPool.AddToHistory(request, RequestStatus.Processed);
		TalkRequests.Remove(request);
	}

	public bool CanDisplayTalk()
	{
		if (pawn.IsPlayer())
		{
			return true;
		}
		if (WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.Planet || Find.CurrentMap == null || pawn.Map != Find.CurrentMap || !pawn.Spawned)
		{
			return false;
		}
		RimTalkSettings settings = Settings.Get();
		if (!settings.DisplayTalkWhenDrafted && pawn.Drafted)
		{
			return false;
		}
		if (!settings.ContinueDialogueWhileSleeping && !pawn.Awake())
		{
			return false;
		}
		return !pawn.Dead && TalkInitiationWeight > 0.0;
	}

	public bool CanGenerateTalk()
	{
		if (pawn.IsPlayer())
		{
			return true;
		}
		return !IsGeneratingTalk && CanDisplayTalk() && pawn.Awake() && TalkResponses.Empty() && CommonUtil.HasPassed(LastTalkTick, 4.0);
	}

	public void IgnoreTalkResponse()
	{
		if (TalkResponses.Count != 0)
		{
			TalkResponse talkResponse = TalkResponses[0];
			TalkHistory.AddIgnored(talkResponse.Id);
			TalkResponses.Remove(talkResponse);
			ApiLog log = ApiHistory.GetApiLog(talkResponse.Id);
			if (log != null)
			{
				log.SpokenTick = -1;
			}
		}
	}

	public void IgnoreAllTalkResponses(List<TalkType> keepTypes = null)
	{
		if (keepTypes == null)
		{
			while (TalkResponses.Count > 0)
			{
				IgnoreTalkResponse();
			}
			return;
		}
		TalkResponses.RemoveAll(delegate(TalkResponse response)
		{
			if (keepTypes.Contains(response.TalkType))
			{
				return false;
			}
			TalkHistory.AddIgnored(response.Id);
			return true;
		});
	}
}
