using System;
using System.Collections.Generic;
using RimWorld;
using Unity.Collections;
using Verse;

namespace TurbojetBackpack;

[StaticConstructorOnStartup]
public static class TurbojetGlobal
{
	[ThreadStatic]
	private static NativeArray<int> fakeWalkableGrid;

	[ThreadStatic]
	private static int cachedGridSize;

	[ThreadStatic]
	public static bool IsCustomPathfinding = false;

	[ThreadStatic]
	private static Dictionary<int, CompTurbojetFlight> _flightCompCache;

	[ThreadStatic]
	private static int _lastCacheTick = -1;

	public static bool SkipReachabilityCheck = false;

	public static NativeArray<int> GetFakeWalkableGrid(int size)
	{
		if (!fakeWalkableGrid.IsCreated || cachedGridSize != size)
		{
			if (fakeWalkableGrid.IsCreated)
			{
				fakeWalkableGrid.Dispose();
			}
			fakeWalkableGrid = new NativeArray<int>(size, Allocator.Persistent);
			cachedGridSize = size;
		}
		return fakeWalkableGrid;
	}

	public static void ClearCache(int pawnId)
	{
		if (_flightCompCache == null)
		{
			_flightCompCache = new Dictionary<int, CompTurbojetFlight>();
		}
		if (_flightCompCache.ContainsKey(pawnId))
		{
			_flightCompCache.Remove(pawnId);
		}
	}

	public static CompTurbojetFlight GetFlightComp(Pawn pawn)
	{
		if (pawn == null)
		{
			return null;
		}
		if (_flightCompCache == null)
		{
			_flightCompCache = new Dictionary<int, CompTurbojetFlight>();
		}
		int ticksGame = GenTicks.TicksGame;
		if (Math.Abs(ticksGame - _lastCacheTick) > 60)
		{
			_flightCompCache.Clear();
			_lastCacheTick = ticksGame;
		}
		int thingIDNumber = pawn.thingIDNumber;
		if (_flightCompCache.TryGetValue(thingIDNumber, out var value))
		{
			if (value != null && value.parent != null && !value.parent.Destroyed)
			{
				return value;
			}
			_flightCompCache.Remove(thingIDNumber);
		}
		if (pawn.apparel == null)
		{
			return null;
		}
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			CompTurbojetFlight comp = item.GetComp<CompTurbojetFlight>();
			if (comp != null)
			{
				_flightCompCache[thingIDNumber] = comp;
				return comp;
			}
		}
		return null;
	}

	public static TurbojetMode GetEffectiveMode(Pawn pawn)
	{
		CompTurbojetFlight flightComp = GetFlightComp(pawn);
		if (flightComp == null)
		{
			return TurbojetMode.Off;
		}
		if (!pawn.Drafted)
		{
			return TurbojetMode.Off;
		}
		return flightComp.FlightMode;
	}

	public static bool IsValidDestination(Pawn pawn, Map map, IntVec3 cell)
	{
		if (pawn == null || map == null)
		{
			return false;
		}
		TurbojetMode effectiveMode = GetEffectiveMode(pawn);
		if (effectiveMode == TurbojetMode.Off)
		{
			return true;
		}
		if (!cell.InBounds(map))
		{
			return false;
		}
		bool flag = map.roofGrid.Roofed(cell);
		Building edifice = cell.GetEdifice(map);
		bool flag2 = edifice != null && (edifice.def.fillPercent >= 1f || edifice.def.passability == Traversability.Impassable);
		bool flag3 = edifice is Building_Door;
		switch (effectiveMode)
		{
		case TurbojetMode.HoverMoving:
			if (flag2 && !flag3)
			{
				return false;
			}
			return true;
		case TurbojetMode.HoverAlways:
			if (flag)
			{
				return false;
			}
			return true;
		default:
			return true;
		}
	}

	public static bool CanPassCell(Pawn pawn, Map map, IntVec3 cell)
	{
		if (pawn == null || map == null)
		{
			return false;
		}
		TurbojetMode effectiveMode = GetEffectiveMode(pawn);
		if (effectiveMode == TurbojetMode.Off)
		{
			return false;
		}
		if (cell == pawn.Position)
		{
			return true;
		}
		bool flag = map.roofGrid.Roofed(cell);
		Building edifice = cell.GetEdifice(map);
		bool flag2 = edifice != null && (edifice.def.fillPercent >= 1f || edifice.def.passability == Traversability.Impassable);
		switch (effectiveMode)
		{
		case TurbojetMode.HoverMoving:
			if (flag2)
			{
				return false;
			}
			return true;
		case TurbojetMode.HoverAlways:
			if (flag)
			{
				return false;
			}
			return true;
		default:
			return false;
		}
	}

	public static bool IsFlightActive(Pawn pawn)
	{
		return GetEffectiveMode(pawn) != TurbojetMode.Off;
	}
}
