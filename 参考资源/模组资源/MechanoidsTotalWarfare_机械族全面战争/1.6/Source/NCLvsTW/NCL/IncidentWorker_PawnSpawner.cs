using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class IncidentWorker_PawnSpawner : IncidentWorker
{
	private ThingDef SpawnThingDef => def.GetModExtension<IncidentDefExtension>()?.spawnThingDef;

	private FactionDef SpawnFaction => def.GetModExtension<IncidentDefExtension>()?.spawnFaction;

	protected override bool CanFireNowSub(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		return map != null && SpawnThingDef != null && map.listerThings.ThingsOfDef(SpawnThingDef).Count == 0;
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		if (map == null)
		{
			return false;
		}
		IntVec2 thingSize = SpawnThingDef.Size;
		int safeMargin = Mathf.Max(thingSize.x, thingSize.z) * 2;
		CellRect safeArea = new CellRect(safeMargin, safeMargin, map.Size.x - safeMargin * 2, map.Size.z - safeMargin * 2);
		bool foundPosition = false;
		foundPosition = TryFindRandomCellInRect(safeArea, map, (IntVec3 c) => IsAreaClearForSpawning(c, thingSize, map), out var spawnCell);
		if (!foundPosition)
		{
			safeMargin = Mathf.Max(thingSize.x, thingSize.z);
			safeArea = new CellRect(safeMargin, safeMargin, map.Size.x - safeMargin * 2, map.Size.z - safeMargin * 2);
			foundPosition = TryFindRandomCellInRect(safeArea, map, (IntVec3 c) => IsAreaClearForSpawning(c, thingSize, map), out spawnCell);
		}
		if (!foundPosition)
		{
			foundPosition = CellFinder.TryFindRandomCell(map, (IntVec3 c) => IsAreaClearForSpawning(c, thingSize, map), out spawnCell);
		}
		if (!foundPosition)
		{
			Log.Error($"无法为 {SpawnThingDef.defName} 找到有效位置。安全区域: {safeArea}");
			return false;
		}
		Thing thing = ThingMaker.MakeThing(SpawnThingDef);
		GenSpawn.Spawn(thing, spawnCell, map);
		SendNotification(parms, thing);
		return true;
	}

	private bool TryFindRandomCellInRect(CellRect rect, Map map, Predicate<IntVec3> validator, out IntVec3 result)
	{
		int maxTries = 100;
		while (maxTries-- > 0)
		{
			IntVec3 randomCell = new IntVec3(Rand.Range(rect.minX, rect.maxX), 0, Rand.Range(rect.minZ, rect.maxZ));
			if (validator(randomCell))
			{
				result = randomCell;
				return true;
			}
		}
		result = IntVec3.Invalid;
		return false;
	}

	private void SendNotification(IncidentParms parms, Thing target)
	{
		try
		{
			SendStandardLetter(def.letterLabel ?? ((string)"警报".Translate()), def.letterText ?? ((string)("检测到 " + target.LabelCap + " 出现").Translate()), def.letterDef ?? LetterDefOf.ThreatBig, parms, new LookTargets(target));
		}
		catch (Exception ex)
		{
			Log.Error($"信件发送失败: {ex}\n{ex.StackTrace}");
		}
	}

	private bool IsAreaClearForSpawning(IntVec3 center, IntVec2 size, Map map)
	{
		CellRect rect = new CellRect(center.x, center.z, size.x, size.z);
		rect.ClipInsideMap(map);
		foreach (IntVec3 cell in rect)
		{
			if (!cell.Standable(map) || cell.GetFirstBuilding(map) != null || cell.Fogged(map))
			{
				return false;
			}
		}
		return true;
	}
}
