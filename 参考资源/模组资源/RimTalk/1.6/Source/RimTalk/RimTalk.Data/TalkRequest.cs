using System;
using System.Collections.Generic;
using RimTalk.Patch;
using RimTalk.Source.Data;
using RimTalk.Util;
using Verse;

namespace RimTalk.Data;

public class TalkRequest
{
	public bool IsMonologue;

	public TalkType TalkType { get; set; }

	public string Context { get; set; }

	public string Prompt { get; set; }

	public Pawn Initiator { get; set; }

	public Pawn Recipient { get; set; }

	public int MapId { get; set; }

	public int CreatedTick { get; set; }

	public DateTime CreatedTime { get; set; }

	public int FinishedTick { get; set; }

	public RequestStatus Status { get; set; }

	public List<Pawn> Participants { get; set; }

	public List<(Role role, string content)> PromptMessages { get; set; }

	public List<PromptMessageSegment> PromptMessageSegments { get; set; }

	public TalkRequest(string prompt, Pawn initiator, Pawn recipient = null, TalkType talkType = TalkType.Other)
	{
		TalkType = talkType;
		Prompt = prompt;
		Initiator = initiator;
		Recipient = recipient;
		CreatedTick = GenTicks.TicksGame;
		CreatedTime = DateTime.Now;
		FinishedTick = -1;
		Status = RequestStatus.Pending;
		base._002Ector();
	}

	public bool IsExpired()
	{
		int duration = 20;
		if (TalkType.IsFromUser())
		{
			return false;
		}
		if (TalkType == TalkType.Urgent)
		{
			duration = 5;
			if (!Initiator.IsInDanger())
			{
				return true;
			}
		}
		else if (TalkType == TalkType.Thought)
		{
			return !ThoughtTracker.IsThoughtStillActive(Initiator, Prompt);
		}
		return GenTicks.TicksGame - CreatedTick > CommonUtil.GetTicksForDuration(duration);
	}

	public TalkRequest Clone()
	{
		return (TalkRequest)MemberwiseClone();
	}
}
