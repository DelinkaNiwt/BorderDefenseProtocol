using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class RadialSearchUtilities
{
	public static IntVec3 FindFirstUnroofed(IntVec3 center, Map map)
	{
		for (int i = 1; i < GenRadial.RadialPattern.Length; i++)
		{
			IntVec3 intVec = GenRadial.RadialPattern[i];
			IntVec3 intVec2 = center + intVec;
			if (intVec2.InBounds(map) && !intVec2.Roofed(map))
			{
				return intVec2;
			}
		}
		return IntVec3.Invalid;
	}

	public static Pawn FindClosestPawn(IntVec3 center, Map map, float maxRange, Predicate<Pawn> validator = null)
	{
		Vector3 vector = center.ToVector3Shifted().Yto0();
		float num = maxRange * maxRange;
		Pawn result = null;
		float num2 = num;
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (validator == null || validator(item))
			{
				float sqrMagnitude = (item.DrawPos.Yto0() - vector).sqrMagnitude;
				if (sqrMagnitude <= num2)
				{
					num2 = sqrMagnitude;
					result = item;
				}
			}
		}
		return result;
	}

	public static List<Pawn> FindClosestPawn(IntVec3 center, Map map, float maxRange, int count, Predicate<Pawn> validator = null)
	{
		Vector3 centerVec = center.ToVector3Shifted().Yto0();
		float maxRange2 = maxRange * maxRange;
		IEnumerable<Pawn> source = from x in (from pawn in map.mapPawns.AllPawnsSpawned
				where validator == null || validator(pawn)
				select new
				{
					Pawn = pawn,
					Dist2 = (pawn.DrawPos.Yto0() - centerVec).sqrMagnitude
				} into x
				where x.Dist2 <= maxRange2
				orderby x.Dist2
				select x).Take(count)
			select x.Pawn;
		return source.ToList();
	}

	public static T FindClosestThingOfType<T>(IntVec3 center, Map map, float maxRange, Predicate<T> validator = null) where T : Thing
	{
		Vector3 vector = center.ToVector3Shifted().Yto0();
		float num = maxRange * maxRange;
		T result = null;
		float num2 = num;
		foreach (T item in map.listerThings.AllThings.OfType<T>())
		{
			if (validator == null || validator(item))
			{
				float sqrMagnitude = (item.DrawPos.Yto0() - vector).sqrMagnitude;
				if (sqrMagnitude <= num2)
				{
					num2 = sqrMagnitude;
					result = item;
				}
			}
		}
		return result;
	}

	public static List<Pawn> FindAllPawnsInRadius(IntVec3 center, Map map, float maxRange, Predicate<Pawn> validator = null)
	{
		float num = maxRange * maxRange;
		List<Pawn> list = new List<Pawn>();
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if ((validator == null || validator(item)) && (item.Position - center).SqrMagnitude <= num)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<T> FindAllThingsOfTypeInRadius<T>(IntVec3 center, Map map, float maxRange, Predicate<T> validator = null, Predicate<IntVec3> cellValidator = null) where T : Thing
	{
		List<T> list = new List<T>();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(center, maxRange, useCenter: true))
		{
			if (!item.InBounds(map) || (cellValidator != null && !cellValidator(item)))
			{
				continue;
			}
			List<Thing> list2 = map.thingGrid.ThingsListAt(item);
			for (int i = 0; i < list2.Count; i++)
			{
				if (list2[i] is T val && (validator == null || validator(val)))
				{
					list.Add(val);
				}
			}
		}
		return list;
	}
}
