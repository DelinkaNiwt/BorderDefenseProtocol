using System;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Data;
using RimTalk.Source.Data;
using RimWorld;
using Verse;

namespace RimTalk.Service;

public class PawnSelector
{
	public enum DetectionType
	{
		Hearing,
		Viewing
	}

	private const float HearingRange = 10f;

	private const float ViewingRange = 20f;

	private static List<Pawn> GetNearbyPawnsInternal(Pawn pawn1, Pawn pawn2 = null, DetectionType detectionType = DetectionType.Hearing, bool onlyTalkable = false, int maxResults = 10)
	{
		float baseRange = ((detectionType == DetectionType.Hearing) ? 10f : 20f);
		PawnCapacityDef capacityDef = ((detectionType == DetectionType.Hearing) ? PawnCapacityDefOf.Hearing : PawnCapacityDefOf.Sight);
		return (from p in (from p in Cache.Keys
				where p != pawn1 && p != pawn2
				where !onlyTalkable || Cache.Get(p).CanGenerateTalk()
				where (double)p.health.capacities.GetLevel(capacityDef) > 0.0
				select p).Where(delegate(Pawn p)
			{
				Room room = p.GetRoom();
				float level = p.health.capacities.GetLevel(capacityDef);
				float maxDist = baseRange * level;
				bool flag = room == pawn1.GetRoom() && p.Position.InHorDistOf(pawn1.Position, maxDist);
				if (pawn2 == null)
				{
					return flag;
				}
				bool flag2 = room == pawn2.GetRoom() && p.Position.InHorDistOf(pawn2.Position, maxDist);
				return flag || flag2;
			})
			orderby (pawn2 == null) ? pawn1.Position.DistanceTo(p.Position) : Math.Min(pawn1.Position.DistanceTo(p.Position), pawn2.Position.DistanceTo(p.Position))
			select p).Take(maxResults).ToList();
	}

	public static List<Pawn> GetNearByTalkablePawns(Pawn pawn1, Pawn pawn2 = null, DetectionType detectionType = DetectionType.Hearing)
	{
		return GetNearbyPawnsInternal(pawn1, pawn2, detectionType, onlyTalkable: true);
	}

	public static List<Pawn> GetAllNearByPawns(Pawn pawn1, Pawn pawn2 = null)
	{
		return GetNearbyPawnsInternal(pawn1, pawn2);
	}

	public static Pawn SelectNextAvailablePawn()
	{
		Pawn pawnWithOldestUserRequest = null;
		int oldestTick = int.MaxValue;
		List<Pawn> talkReadyPawns = new List<Pawn>();
		foreach (Pawn pawn in Cache.Keys)
		{
			PawnState pawnState = Cache.Get(pawn);
			int minTick = (from req in pawnState.TalkRequests
				where req.TalkType == TalkType.User
				select req.CreatedTick).DefaultIfEmpty(int.MaxValue).Min();
			if (minTick < oldestTick)
			{
				oldestTick = minTick;
				pawnWithOldestUserRequest = pawn;
			}
			if (pawnState.CanGenerateTalk())
			{
				talkReadyPawns.Add(pawn);
			}
		}
		return pawnWithOldestUserRequest ?? (talkReadyPawns.Any() ? Cache.GetRandomWeightedPawn(talkReadyPawns) : null);
	}
}
