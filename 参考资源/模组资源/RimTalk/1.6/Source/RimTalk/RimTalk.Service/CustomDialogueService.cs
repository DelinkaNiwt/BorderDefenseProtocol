using System.Collections.Generic;
using RimTalk.Data;
using RimTalk.Source.Data;
using RimTalk.UI;
using RimTalk.Util;
using Verse;

namespace RimTalk.Service;

public static class CustomDialogueService
{
	public class PendingDialogue(Pawn recipient, string message)
	{
		public readonly Pawn Recipient = recipient;

		public readonly string Message = message;
	}

	private const float TalkDistance = 20f;

	public static readonly Dictionary<Pawn, PendingDialogue> PendingDialogues = new Dictionary<Pawn, PendingDialogue>();

	public static void Tick()
	{
		List<Pawn> toRemove = new List<Pawn>();
		foreach (var (initiator, dialogue) in PendingDialogues)
		{
			if (initiator == null || initiator.Destroyed || dialogue.recipient == null || dialogue.recipient.Destroyed)
			{
				toRemove.Add(initiator);
			}
			else if (CanTalk(initiator, dialogue.recipient))
			{
				ExecuteDialogue(initiator, dialogue.recipient, dialogue.message);
				toRemove.Add(initiator);
			}
		}
		foreach (Pawn pawn2 in toRemove)
		{
			PendingDialogues.Remove(pawn2);
		}
	}

	private static bool InSameRoom(Pawn pawn1, Pawn pawn2)
	{
		Room room1 = pawn1.GetRoom();
		Room room2 = pawn2.GetRoom();
		return (room1 != null && room2 != null && room1 == room2) || (room1 == null && room2 == null);
	}

	public static bool CanTalk(Pawn initiator, Pawn recipient)
	{
		if (initiator.IsPlayer())
		{
			return true;
		}
		float distance = initiator.Position.DistanceTo(recipient.Position);
		return distance <= 20f && InSameRoom(initiator, recipient);
	}

	public static void ExecuteDialogue(Pawn initiator, Pawn recipient, string message)
	{
		PawnState initiatorState = Cache.Get(initiator);
		if (initiatorState != null && initiatorState.CanDisplayTalk())
		{
			PawnState recipientState = Cache.Get(recipient);
			if (recipientState != null && recipientState.CanDisplayTalk())
			{
				recipientState.AddTalkRequest(message, initiator, TalkType.User);
			}
			ApiLog apiLog = ApiHistory.AddUserHistory(initiator, recipient, message);
			if (initiator.IsPlayer())
			{
				apiLog.SpokenTick = GenTicks.TicksGame;
				Overlay.NotifyLogUpdated();
			}
			else
			{
				TalkResponse talkResponse = new TalkResponse(TalkType.User, initiator.LabelShort, message)
				{
					Id = apiLog.Id
				};
				Cache.Get(initiator).TalkResponses.Insert(0, talkResponse);
			}
		}
	}
}
