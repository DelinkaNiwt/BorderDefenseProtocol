using System.Collections.Generic;
using System.Linq;
using RimTalk.Source.Data;
using Verse;

namespace RimTalk.Data;

public static class TalkRequestPool
{
	private static readonly List<TalkRequest> Requests = new List<TalkRequest>();

	private static readonly List<TalkRequest> History = new List<TalkRequest>();

	private const int MaxHistorySize = 500;

	public static int Count => Requests.Count;

	public static bool IsEmpty => Requests.Count == 0;

	public static void Add(string prompt, Pawn initiator = null, Pawn recipient = null, int mapId = 0, TalkType talkType = TalkType.Event)
	{
		TalkRequest request = new TalkRequest(prompt, initiator, recipient, talkType)
		{
			MapId = mapId,
			Status = RequestStatus.Pending
		};
		Requests.Add(request);
	}

	public static TalkRequest GetRequestFromPool(Pawn pawn)
	{
		for (int i = Requests.Count - 1; i >= 0; i--)
		{
			TalkRequest request = Requests[i];
			if (request.MapId == pawn.Map.uniqueID)
			{
				if (!request.IsExpired())
				{
					request.Initiator = pawn;
					return request;
				}
				AddToHistory(request, RequestStatus.Expired);
				Requests.RemoveAt(i);
			}
		}
		return null;
	}

	public static void AddToHistory(TalkRequest request, RequestStatus status)
	{
		request.Status = status;
		if (request.FinishedTick == -1)
		{
			request.FinishedTick = GenTicks.TicksGame;
		}
		if (!History.Contains(request))
		{
			History.Add(request);
			if (History.Count > 500)
			{
				History.RemoveAt(0);
			}
		}
	}

	public static bool Remove(TalkRequest request)
	{
		if (Requests.Contains(request))
		{
			AddToHistory(request, RequestStatus.Processed);
			Requests.Remove(request);
			return true;
		}
		return false;
	}

	public static IEnumerable<TalkRequest> GetAllActive()
	{
		return Requests.ToList();
	}

	public static IEnumerable<TalkRequest> GetHistory()
	{
		return History.ToList();
	}

	public static void Clear()
	{
		Requests.Clear();
		History.Clear();
	}

	public static void ClearHistory()
	{
		History.Clear();
	}
}
