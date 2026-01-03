using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FCI_Milira;

public static class AreaFinder
{
	public static bool ConeShapedArea(IntVec3 startPos, IntVec3 targetPos, float sectorAngle, float directionAngle, float radius)
	{
		Vector3 v = (targetPos - startPos).ToVector3();
		float num = v.MagnitudeHorizontal();
		if (radius > 0f && num > radius)
		{
			return false;
		}
		v.Normalize();
		float angle = v.ToAngleFlat();
		float angle2 = directionAngle - sectorAngle / 2f;
		float angle3 = directionAngle + sectorAngle / 2f;
		angle = NormalizeAngle360(angle);
		angle2 = NormalizeAngle360(angle2);
		angle3 = NormalizeAngle360(angle3);
		if (angle2 <= angle3)
		{
			return angle >= angle2 && angle <= angle3;
		}
		return angle >= angle2 || angle <= angle3;
	}

	private static float NormalizeAngle360(float angle)
	{
		angle %= 360f;
		if (angle < 0f)
		{
			angle += 360f;
		}
		return angle;
	}

	public static IntVec3 GetRandomDropSpot(Map map, bool useTradeDropSpot, bool allowFogged)
	{
		if (useTradeDropSpot)
		{
			return DropCellFinder.TradeDropSpot(map);
		}
		if (CellFinderLoose.TryGetRandomCellWith((IntVec3 x) => x.Standable(map) && !x.Roofed(map) && (allowFogged || !x.Fogged(map)) && map.reachability.CanReachColony(x), map, 1000, out var result))
		{
			return result;
		}
		return DropCellFinder.RandomDropSpot(map);
	}

	public static IntVec3 FindNearEdgeCell(Map map, Predicate<IntVec3> extraCellValidator)
	{
		TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassDoors);
		Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && !x.Fogged(map) && map.reachability.CanReachMapEdge(x, traverseParams) && map.reachability.CanReach(x, MapGenerator.PlayerStartSpot, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors));
		if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)), map, CellFinder.EdgeRoadChance_Neutral, out var result))
		{
			return CellFinder.RandomClosewalkCellNear(result, map, 5);
		}
		if (CellFinder.TryFindRandomEdgeCellWith(baseValidator, map, CellFinder.EdgeRoadChance_Neutral, out result))
		{
			return CellFinder.RandomClosewalkCellNear(result, map, 5);
		}
		Log.Warning("Could not find any valid edge cell connected to PlayerStartSpot. Using random cell instead.");
		return CellFinder.RandomCell(map);
	}
}
