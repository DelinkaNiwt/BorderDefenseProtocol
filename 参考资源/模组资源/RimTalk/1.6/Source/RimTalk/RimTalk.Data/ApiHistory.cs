using System;
using System.Collections.Generic;
using RimTalk.Client;
using RimTalk.Source.Data;
using Verse;

namespace RimTalk.Data;

public static class ApiHistory
{
	private static readonly Dictionary<Guid, ApiLog> History = new Dictionary<Guid, ApiLog>();

	private static int _conversationIdIndex = 0;

	public static ApiLog GetApiLog(Guid id)
	{
		ApiLog apiLog;
		return History.TryGetValue(id, out apiLog) ? apiLog : null;
	}

	public static ApiLog AddRequest(TalkRequest request, Channel channel)
	{
		ApiLog log = new ApiLog(request.Initiator.LabelShort, request, null, null, DateTime.Now, channel)
		{
			IsFirstDialogue = true,
			ConversationId = (request.IsMonologue ? (-1) : _conversationIdIndex++)
		};
		History[log.Id] = log;
		return log;
	}

	public static void UpdatePayload(Guid id, Payload payload)
	{
		if (History.TryGetValue(id, out var log))
		{
			log.Payload = payload;
		}
	}

	public static ApiLog AddResponse(Guid id, string response, string name, string interactionType, Payload payload = null, int elapsedMs = 0)
	{
		if (!History.TryGetValue(id, out var originalLog))
		{
			return null;
		}
		if (originalLog.Response == null)
		{
			originalLog.Name = name ?? originalLog.Name;
			originalLog.Response = response;
			originalLog.InteractionType = interactionType;
			originalLog.Payload = payload;
			originalLog.ElapsedMs = (int)(DateTime.Now - originalLog.Timestamp).TotalMilliseconds;
			return originalLog;
		}
		ApiLog newLog = new ApiLog(name, originalLog.TalkRequest, response, payload, DateTime.Now, originalLog.Channel);
		History[newLog.Id] = newLog;
		newLog.InteractionType = interactionType;
		newLog.ElapsedMs = elapsedMs;
		newLog.ConversationId = originalLog.ConversationId;
		return newLog;
	}

	public static ApiLog AddUserHistory(Pawn initiator, Pawn recipient, string text)
	{
		string prompt = initiator.LabelShort + " talked to " + recipient.LabelShort;
		TalkRequest talkRequest = new TalkRequest(prompt, initiator, recipient, TalkType.User);
		ApiLog log = new ApiLog(initiator.LabelShort, talkRequest, text, null, DateTime.Now, Channel.User);
		History[log.Id] = log;
		return log;
	}

	public static IEnumerable<ApiLog> GetAll()
	{
		foreach (KeyValuePair<Guid, ApiLog> item in History)
		{
			yield return item.Value;
		}
	}

	public static void Clear()
	{
		History.Clear();
	}
}
