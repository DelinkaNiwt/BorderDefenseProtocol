using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using VEF.CacheClearing;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch]
public static class GenRadialCached
{
	private readonly struct Key : IEquatable<Key>
	{
		public readonly IntVec3 loc;

		public readonly float radius;

		public readonly int mapId;

		public readonly bool useCenter;

		public Key(IntVec3 loc, float radius, int mapId, bool useCenter)
		{
			this.loc = loc;
			this.radius = radius;
			this.mapId = mapId;
			this.useCenter = useCenter;
		}

		public Key DecrementMapId()
		{
			return new Key(loc, radius, mapId - 1, useCenter);
		}

		public bool Equals(Key other)
		{
			if (loc.Equals(other.loc) && radius.Equals(other.radius) && mapId == other.mapId)
			{
				return useCenter == other.useCenter;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Key other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineInt(loc.GetHashCode(), radius.GetHashCode(), mapId, useCenter.GetHashCode());
		}
	}

	private static Dictionary<Key, HashSet<Thing>> cache;

	private static Dictionary<Key, HashSet<CompMeditationFocus>> meditationFocusCache;

	private static Dictionary<Key, float> wealthCache;

	static GenRadialCached()
	{
		cache = new Dictionary<Key, HashSet<Thing>>();
		meditationFocusCache = new Dictionary<Key, HashSet<CompMeditationFocus>>();
		wealthCache = new Dictionary<Key, float>();
		ClearCaches.clearCacheTypes.Add(typeof(GenRadialCached));
	}

	public static IEnumerable<Thing> RadialDistinctThingsAround(IntVec3 center, Map map, float radius, bool useCenter)
	{
		Key key = new Key(center, radius, map.Index, useCenter);
		return RadialDistinctThingsAround(in key, map);
	}

	private static IEnumerable<Thing> RadialDistinctThingsAround(ref readonly Key key, Map map)
	{
		if (cache == null)
		{
			cache = new Dictionary<Key, HashSet<Thing>>();
		}
		if (cache.TryGetValue(key, out var value))
		{
			return value;
		}
		value = new HashSet<Thing>();
		int num = GenRadial.NumCellsInRadius(key.radius);
		for (int i = ((!key.useCenter) ? 1 : 0); i < num; i++)
		{
			IntVec3 c = GenRadial.RadialPattern[i] + key.loc;
			if (c.InBounds(map))
			{
				value.UnionWith(c.GetThingList(map));
			}
		}
		cache[key] = value;
		return value;
	}

	public static float WealthAround(IntVec3 center, Map map, float radius, bool useCenter)
	{
		Key key = new Key(center, radius, map.Index, useCenter);
		if (wealthCache == null)
		{
			wealthCache = new Dictionary<Key, float>();
		}
		if (wealthCache.TryGetValue(key, out var value))
		{
			return value;
		}
		IEnumerable<Thing> enumerable = RadialDistinctThingsAround(in key, map);
		float num = 0f;
		foreach (Thing item in enumerable)
		{
			num += item.GetStatValue(StatDefOf.MarketValue) * (float)item.stackCount;
		}
		wealthCache[key] = num;
		return num;
	}

	public static IEnumerable<CompMeditationFocus> MeditationFociAround(IntVec3 center, Map map, float radius, bool useCenter)
	{
		Key key = new Key(center, radius, map.Index, useCenter);
		if (meditationFocusCache == null)
		{
			meditationFocusCache = new Dictionary<Key, HashSet<CompMeditationFocus>>();
		}
		if (meditationFocusCache.TryGetValue(key, out var value))
		{
			return value;
		}
		value = new HashSet<CompMeditationFocus>();
		foreach (Thing item in RadialDistinctThingsAround(in key, map))
		{
			CompMeditationFocus compMeditationFocus = item.TryGetComp<CompMeditationFocus>();
			if (compMeditationFocus != null)
			{
				value.Add(compMeditationFocus);
			}
		}
		meditationFocusCache[key] = value;
		return value;
	}

	[HarmonyPatch(typeof(Thing), "SpawnSetup")]
	[HarmonyPostfix]
	public static void SpawnSetup_Postfix(Thing __instance)
	{
		ClearCacheFor(__instance);
	}

	[HarmonyPatch(typeof(Thing), "DeSpawn")]
	[HarmonyPrefix]
	public static void DeSpawn_Prefix(Thing __instance)
	{
		ClearCacheFor(__instance);
	}

	[HarmonyPatch(typeof(MapDeiniter), "Deinit")]
	[HarmonyPostfix]
	public static void Deinit_Postfix(Map map)
	{
		int index = map.Index;
		foreach (var (key2, value) in cache.ToList())
		{
			if (key2.mapId < index)
			{
				continue;
			}
			cache.Remove(key2);
			if (meditationFocusCache.TryGetValue(key2, out var value2))
			{
				meditationFocusCache.Remove(key2);
			}
			if (wealthCache.TryGetValue(key2, out var value3))
			{
				wealthCache.Remove(key2);
			}
			else
			{
				value3 = float.NaN;
			}
			if (key2.mapId != index)
			{
				Key key3 = key2.DecrementMapId();
				cache.Add(key3, value);
				if (value2 != null)
				{
					meditationFocusCache.Add(key3, value2);
				}
				if (!float.IsNaN(value3))
				{
					wealthCache.Add(key3, value3);
				}
			}
		}
	}

	private static void ClearCacheFor(Thing thing)
	{
		if (!thing.Spawned)
		{
			return;
		}
		cache.RemoveAll(delegate(KeyValuePair<Key, HashSet<Thing>> pair)
		{
			if (pair.Key.mapId != thing.Map.Index || !thing.OccupiedRect().ClosestCellTo(pair.Key.loc).InHorDistOf(pair.Key.loc, pair.Key.radius))
			{
				return false;
			}
			meditationFocusCache.Remove(pair.Key);
			wealthCache.Remove(pair.Key);
			return true;
		});
	}
}
