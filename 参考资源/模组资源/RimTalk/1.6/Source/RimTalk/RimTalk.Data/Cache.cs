using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalk.Data;

public static class Cache
{
	private static readonly ConcurrentDictionary<Pawn, PawnState> PawnCache = new ConcurrentDictionary<Pawn, PawnState>();

	private static readonly ConcurrentDictionary<string, Pawn> NameCache = new ConcurrentDictionary<string, Pawn>();

	private static readonly Random Random = new Random();

	private static Pawn _playerPawn;

	public static IEnumerable<Pawn> Keys => PawnCache.Keys;

	public static Pawn GetPlayer()
	{
		return _playerPawn;
	}

	public static PawnState Get(Pawn pawn)
	{
		if (pawn == null)
		{
			return null;
		}
		if (PawnCache.TryGetValue(pawn, out var state))
		{
			return state;
		}
		if (!pawn.IsTalkEligible())
		{
			return null;
		}
		PawnCache[pawn] = new PawnState(pawn);
		NameCache[pawn.LabelShort] = pawn;
		return PawnCache[pawn];
	}

	public static PawnState GetByName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Pawn pawn;
		return NameCache.TryGetValue(name, out pawn) ? Get(pawn) : null;
	}

	public static void Refresh()
	{
		Pawn value;
		foreach (Pawn pawn in PawnCache.Keys.ToList())
		{
			if (!pawn.IsTalkEligible())
			{
				if (PawnCache.TryRemove(pawn, out var removedState))
				{
					NameCache.TryRemove(removedState.Pawn.LabelShort, out value);
				}
				continue;
			}
			string label = pawn.LabelShort;
			if (!string.IsNullOrEmpty(label))
			{
				NameCache[label] = pawn;
			}
		}
		InitializePlayerPawn();
		KeyValuePair<string, Pawn>[] array = NameCache.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, Pawn> entry = array[i];
			Pawn pawn2 = entry.Value;
			if (pawn2 == null || !PawnCache.ContainsKey(pawn2) || pawn2.LabelShort != entry.Key)
			{
				NameCache.TryRemove(entry.Key, out value);
			}
		}
		foreach (Pawn pawn3 in Find.CurrentMap.mapPawns.AllPawnsSpawned)
		{
			if (pawn3.IsTalkEligible() && !PawnCache.ContainsKey(pawn3))
			{
				PawnCache[pawn3] = new PawnState(pawn3);
				NameCache[pawn3.LabelShort] = pawn3;
			}
		}
	}

	public static IEnumerable<PawnState> GetAll()
	{
		return PawnCache.Values;
	}

	public static void Clear()
	{
		PawnCache.Clear();
		NameCache.Clear();
		_playerPawn = null;
	}

	private static double GetScaleFactor(double groupWeight, double baselineWeight)
	{
		if (baselineWeight <= 0.0 || groupWeight <= 0.0)
		{
			return 0.0;
		}
		if (groupWeight > baselineWeight)
		{
			return baselineWeight / groupWeight;
		}
		return 1.0;
	}

	public static Pawn GetRandomWeightedPawn(IEnumerable<Pawn> pawns)
	{
		List<Pawn> pawnList = pawns.ToList();
		if (pawnList.NullOrEmpty())
		{
			return null;
		}
		double totalColonistWeight = 0.0;
		double totalVisitorWeight = 0.0;
		double totalEnemyWeight = 0.0;
		double totalSlaveWeight = 0.0;
		double totalPrisonerWeight = 0.0;
		foreach (Pawn p in pawnList)
		{
			double weight = Get(p)?.TalkInitiationWeight ?? 0.0;
			if (p.IsFreeNonSlaveColonist || p.HasVocalLink())
			{
				totalColonistWeight += weight;
			}
			else if (p.IsSlave)
			{
				totalSlaveWeight += weight;
			}
			else if (p.IsPrisoner)
			{
				totalPrisonerWeight += weight;
			}
			else if (p.IsVisitor())
			{
				totalVisitorWeight += weight;
			}
			else if (p.IsEnemy())
			{
				totalEnemyWeight += weight;
			}
		}
		double baselineWeight = ((!(totalColonistWeight > 0.0)) ? new double[4] { totalVisitorWeight, totalEnemyWeight, totalSlaveWeight, totalPrisonerWeight }.Max() : totalColonistWeight);
		if (baselineWeight <= 0.0)
		{
			return null;
		}
		double colonistScaleFactor = GetScaleFactor(totalColonistWeight, baselineWeight);
		double visitorScaleFactor = GetScaleFactor(totalVisitorWeight, baselineWeight);
		double enemyScaleFactor = GetScaleFactor(totalEnemyWeight, baselineWeight);
		double slaveScaleFactor = GetScaleFactor(totalSlaveWeight, baselineWeight);
		double prisonerScaleFactor = GetScaleFactor(totalPrisonerWeight, baselineWeight);
		double effectiveTotalWeight = totalColonistWeight * colonistScaleFactor + totalVisitorWeight * visitorScaleFactor + totalEnemyWeight * enemyScaleFactor + totalSlaveWeight * slaveScaleFactor + totalPrisonerWeight * prisonerScaleFactor;
		if (effectiveTotalWeight <= 0.0)
		{
			return null;
		}
		if (effectiveTotalWeight < 1.0 && Random.NextDouble() > effectiveTotalWeight)
		{
			return null;
		}
		double randomWeight = Random.NextDouble() * effectiveTotalWeight;
		double cumulativeWeight = 0.0;
		foreach (Pawn pawn in pawnList)
		{
			double currentPawnWeight = Get(pawn)?.TalkInitiationWeight ?? 0.0;
			double currentEffectiveWeight = 0.0;
			if (pawn.IsFreeNonSlaveColonist || pawn.HasVocalLink())
			{
				currentEffectiveWeight = currentPawnWeight * colonistScaleFactor;
			}
			else if (pawn.IsSlave)
			{
				currentEffectiveWeight = currentPawnWeight * slaveScaleFactor;
			}
			else if (pawn.IsPrisoner)
			{
				currentEffectiveWeight = currentPawnWeight * prisonerScaleFactor;
			}
			else if (pawn.IsVisitor())
			{
				currentEffectiveWeight = currentPawnWeight * visitorScaleFactor;
			}
			else if (pawn.IsEnemy())
			{
				currentEffectiveWeight = currentPawnWeight * enemyScaleFactor;
			}
			cumulativeWeight += currentEffectiveWeight;
			if (randomWeight < cumulativeWeight)
			{
				return pawn;
			}
		}
		return pawnList.LastOrDefault((Pawn pawn2) => (Get(pawn2)?.TalkInitiationWeight ?? 0.0) > 0.0);
	}

	public static void InitializePlayerPawn()
	{
		if (Current.Game != null && !(Settings.Get().PlayerName == _playerPawn?.Name.ToStringShort))
		{
			_playerPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
			_playerPawn.Name = new NameSingle(Settings.Get().PlayerName);
			PawnCache[_playerPawn] = new PawnState(_playerPawn);
			NameCache[_playerPawn.LabelShort] = _playerPawn;
		}
	}
}
