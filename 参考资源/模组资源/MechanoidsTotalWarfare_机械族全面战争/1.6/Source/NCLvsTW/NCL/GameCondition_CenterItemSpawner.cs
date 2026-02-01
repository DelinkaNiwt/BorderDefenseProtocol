using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class GameCondition_CenterItemSpawner : GameCondition
{
	private const int SpawnIntervalTicks = 3600;

	private const int SpawnRadius = 50;

	private int nextSpawnTick;

	private static readonly List<string> itemDefNames = new List<string> { "B2000MechLF", "B2000MechUD" };

	public override string TooltipString
	{
		get
		{
			string baseString = base.TooltipString;
			string spawnInfo = "Next item spawn: " + (nextSpawnTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
			return string.IsNullOrEmpty(baseString) ? spawnInfo : (baseString + "\n" + spawnInfo);
		}
	}

	public override void Init()
	{
		base.Init();
		nextSpawnTick = Find.TickManager.TicksGame + 3600;
	}

	public override void GameConditionTick()
	{
		base.GameConditionTick();
		if (Find.TickManager.TicksGame < nextSpawnTick)
		{
			return;
		}
		foreach (Map map in base.AffectedMaps)
		{
			TrySpawnRandomItem(map);
		}
		nextSpawnTick = Find.TickManager.TicksGame + 3600;
	}

	private void TrySpawnRandomItem(Map map)
	{
		ThingDef itemDef = GetRandomItemDef();
		if (itemDef != null)
		{
			if (!TryFindSpawnPosition(map, out var spawnPos))
			{
				Log.Warning($"[CenterItemSpawner] Failed to find valid spawn position on map {map}");
				return;
			}
			Thing item = ThingMaker.MakeThing(itemDef);
			item.stackCount = 1;
			GenPlace.TryPlaceThing(item, spawnPos, map, ThingPlaceMode.Direct);
		}
	}

	private ThingDef GetRandomItemDef()
	{
		string defName = itemDefNames.RandomElement();
		ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
		if (def == null)
		{
			Log.Error("[CenterItemSpawner] Missing item definition: " + defName);
			return null;
		}
		return def;
	}

	private bool TryFindSpawnPosition(Map map, out IntVec3 result)
	{
		IntVec3 center = map.Center;
		Predicate<IntVec3> validator = (IntVec3 c) => c.Standable(map) && !c.Fogged(map) && c.GetFirstItem(map) == null;
		return CellFinder.TryFindRandomCellNear(center, map, 50, validator, out result);
	}
}
