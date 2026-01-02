using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class IncidentWorker_MeteoriteImpact_SolarCrystal : IncidentWorker
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		IntVec3 cell;
		return TryFindCell(out cell, map);
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		if (!TryFindCell(out var cell, map))
		{
			return false;
		}
		List<Thing> list = new List<Thing>();
		for (int i = 0; i < Rand.Range(4, 14); i++)
		{
			Thing item = ThingMaker.MakeThing(MiliraDefOf.Milira_SolarCrystalDruse);
			list.Add(item);
		}
		SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, list, cell, map);
		SendStandardLetter(def.letterLabel, def.letterText, LetterDefOf.PositiveEvent, parms, new TargetInfo(cell, map));
		return true;
	}

	private bool TryFindCell(out IntVec3 cell, Map map)
	{
		int maxMineables = ThingSetMaker_Meteorite.MineablesCountRange.max;
		return CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.MeteoriteIncoming, map, TerrainAffordanceDefOf.Light, out cell, 10, default(IntVec3), -1, allowRoofedCells: true, allowCellsWithItems: false, allowCellsWithBuildings: false, colonyReachable: false, avoidColonistsIfExplosive: true, alwaysAvoidColonists: true, delegate(IntVec3 x)
		{
			int num = Mathf.CeilToInt(Mathf.Sqrt(maxMineables)) + 2;
			CellRect cellRect = CellRect.CenteredOn(x, num, num);
			int num2 = 0;
			foreach (IntVec3 item in cellRect)
			{
				if (item.InBounds(map) && item.Standable(map))
				{
					num2++;
				}
			}
			return num2 >= maxMineables;
		});
	}
}
