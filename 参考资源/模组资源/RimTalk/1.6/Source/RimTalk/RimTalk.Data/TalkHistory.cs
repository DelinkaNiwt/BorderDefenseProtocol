using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTalk.Data;

public static class TalkHistory
{
	private static readonly ConcurrentDictionary<int, List<(Role role, string message)>> MessageHistory = new ConcurrentDictionary<int, List<(Role, string)>>();

	private static readonly ConcurrentDictionary<Guid, int> SpokenTickCache = new ConcurrentDictionary<Guid, int> { [Guid.Empty] = 0 };

	private static readonly ConcurrentBag<Guid> IgnoredCache = new ConcurrentBag<Guid>();

	public static void AddSpoken(Guid id)
	{
		SpokenTickCache.TryAdd(id, GenTicks.TicksGame);
	}

	public static void AddIgnored(Guid id)
	{
		IgnoredCache.Add(id);
	}

	public static int GetSpokenTick(Guid id)
	{
		int tick;
		return SpokenTickCache.TryGetValue(id, out tick) ? tick : (-1);
	}

	public static bool IsTalkIgnored(Guid id)
	{
		return IgnoredCache.Contains(id);
	}

	public static void AddMessageHistory(Pawn pawn, string request, string response)
	{
		List<(Role, string)> messages = MessageHistory.GetOrAdd(pawn.thingIDNumber, (int _) => new List<(Role, string)>());
		lock (messages)
		{
			messages.Add((Role.User, request));
			messages.Add((Role.AI, response));
			EnsureMessageLimit(messages);
		}
	}

	public static List<(Role role, string message)> GetMessageHistory(Pawn pawn)
	{
		if (!MessageHistory.TryGetValue(pawn.thingIDNumber, out List<(Role, string)> history))
		{
			return new List<(Role, string)>();
		}
		lock (history)
		{
			return history.ToList();
		}
	}

	private static void EnsureMessageLimit(List<(Role role, string message)> messages)
	{
		for (int i = messages.Count - 1; i > 0; i--)
		{
			if (messages[i].role == messages[i - 1].role)
			{
				messages.RemoveAt(i - 1);
			}
		}
		int maxMessages = Settings.Get().Context.ConversationHistoryCount;
		while (messages.Count > maxMessages * 2)
		{
			messages.RemoveAt(0);
		}
	}

	public static void Clear()
	{
		MessageHistory.Clear();
	}
}
