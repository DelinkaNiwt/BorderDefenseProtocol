using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public static class AncotPrefabUtility
{
	public static CellRect GetPrefabRect(PrefabDef prefab, IntVec3 pos, Rot4 rot, Map map)
	{
		return GenAdj.OccupiedRect(pos, rot, prefab.size).ClipInsideMap(map);
	}

	public static void SpawnPrefabRoofed(PrefabDef prefab, RoofDef roof, out CellRect cellRect, Map map, IntVec3 pos, Rot4 rot, Faction faction = null, List<Thing> spawned = null, Func<PrefabThingData, Tuple<ThingDef, ThingDef>> overrideSpawnData = null, Action<Thing> onSpawned = null, bool blueprint = false)
	{
		PrefabUtility.SpawnPrefab(prefab, map, pos, rot, faction, spawned, overrideSpawnData, onSpawned, blueprint);
		cellRect = GetPrefabRect(prefab, pos, rot, map);
		SetRoofForRoom(cellRect, roof, map);
	}

	public static void ClearPrefabArea(PrefabDef prefab, Map map, IntVec3 pos, Rot4 rot, TerrainDef terrainForced = null, bool clearPawn = true)
	{
		rot = PrefabUtility.ValidateRotation(prefab, rot);
		IntVec3 root = PrefabUtility.GetRoot(prefab, pos, rot);
		foreach (var thing in prefab.GetThings())
		{
			PrefabThingData item = thing.data;
			IntVec3 item2 = thing.cell;
			IntVec3 adjustedThingLocalPosition = PrefabUtility.GetAdjustedThingLocalPosition(item, rot, item2);
			IntVec3 c = root + adjustedThingLocalPosition;
			map.roofGrid.SetRoof(c, null);
			foreach (Thing item5 in c.GetThingList(map).ToList())
			{
				if (item5.def.destroyable && (!(item5 is Pawn) || clearPawn))
				{
					item5.Destroy();
				}
			}
		}
		foreach (var item6 in prefab.GetTerrain())
		{
			PrefabTerrainData item3 = item6.data;
			IntVec3 item4 = item6.cell;
			IntVec3 adjustedLocalPosition = PrefabUtility.GetAdjustedLocalPosition(item4, rot);
			IntVec3 c2 = root + adjustedLocalPosition;
			map.roofGrid.SetRoof(c2, null);
			foreach (Thing item7 in c2.GetThingList(map).ToList())
			{
				if (item7.def.destroyable && (!(item7 is Pawn) || clearPawn))
				{
					item7.Destroy();
				}
			}
			if (terrainForced != null)
			{
				map.terrainGrid.SetTerrain(c2, terrainForced);
			}
		}
	}

	public static void SetRoofForRoom(CellRect region, RoofDef roof, Map map, bool clearFog = true)
	{
		HashSet<Room> hashSet = new HashSet<Room>();
		foreach (IntVec3 cell in region.Cells)
		{
			if (!cell.InBounds(map) || cell.Roofed(map))
			{
				continue;
			}
			Room room = cell.GetRoom(map);
			if (room == null || hashSet.Contains(room) || room.PsychologicallyOutdoors || room.TouchesMapEdge || room.CellCount <= 1)
			{
				continue;
			}
			hashSet.Add(room);
			foreach (IntVec3 cell2 in room.Cells)
			{
				if (cell2.InBounds(map))
				{
					map.roofGrid.SetRoof(cell2, roof);
				}
			}
			foreach (IntVec3 cell3 in room.Cells)
			{
				IntVec3[] adjacentCellsAndInside = GenAdj.AdjacentCellsAndInside;
				foreach (IntVec3 intVec in adjacentCellsAndInside)
				{
					IntVec3 intVec2 = cell3 + intVec;
					if (intVec2.InBounds(map) && !room.ContainsCell(intVec2))
					{
						Building edifice = intVec2.GetEdifice(map);
						if (edifice != null && edifice.def.category == ThingCategory.Building && edifice.def.holdsRoof)
						{
							map.roofGrid.SetRoof(intVec2, roof);
						}
					}
				}
			}
		}
	}

	public static void GravShip_AddPrefab(this Sketch sketch, PrefabDef prefab, IntVec3 pos, Rot4 rot)
	{
		rot = PrefabUtility.ValidateRotation(prefab, rot);
		IntVec3 root = PrefabUtility.GetRoot(prefab, pos, rot);
		foreach (var (prefabThingData, cell) in prefab.GetThings())
		{
			if (prefabThingData.def == ThingDefOf.GravEngine)
			{
				IntVec3 adjustedThingLocalPosition = PrefabUtility.GetAdjustedThingLocalPosition(prefabThingData, rot, cell);
				sketch.AddThing(prefabThingData.def, root + adjustedThingLocalPosition, rot.Rotated(prefabThingData.relativeRotation), prefabThingData.stuff, prefabThingData.stackCountRange.RandomInRange, null, null, wipeIfCollides: true, 0.5f);
			}
		}
		foreach (var (prefabTerrainData, local) in prefab.GetTerrain())
		{
			if (Rand.Chance(prefabTerrainData.chance))
			{
				IntVec3 adjustedLocalPosition = PrefabUtility.GetAdjustedLocalPosition(local, rot);
				sketch.AddTerrain(prefabTerrainData.def, root + adjustedLocalPosition);
			}
		}
		foreach (var (prefabThingData2, cell2) in prefab.GetThings())
		{
			if (prefabThingData2.def != ThingDefOf.GravEngine)
			{
				IntVec3 adjustedThingLocalPosition2 = PrefabUtility.GetAdjustedThingLocalPosition(prefabThingData2, rot, cell2);
				sketch.AddThing(prefabThingData2.def, root + adjustedThingLocalPosition2, rot.Rotated(prefabThingData2.relativeRotation), prefabThingData2.stuff, prefabThingData2.stackCountRange.RandomInRange);
			}
		}
		foreach (var (subPrefabData, cell3) in prefab.GetPrefabs())
		{
			if (Rand.Chance(subPrefabData.chance))
			{
				IntVec3 adjustedPrefabLocalPosition = PrefabUtility.GetAdjustedPrefabLocalPosition(subPrefabData, rot, cell3);
				sketch.AddPrefab(subPrefabData.def, root + adjustedPrefabLocalPosition, rot.Rotated(subPrefabData.relativeRotation));
			}
		}
	}
}
