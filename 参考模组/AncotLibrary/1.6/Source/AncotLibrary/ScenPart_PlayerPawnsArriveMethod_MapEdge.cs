using System;
using System.Collections.Generic;
using System.Linq;
using FCI_Milira;
using RimWorld;
using RimWorld.SketchGen;
using Verse;

namespace AncotLibrary;

public class ScenPart_PlayerPawnsArriveMethod_MapEdge : ScenPart_PlayerPawnsArriveMethod
{
	private PlayerPawnsArriveMethod method;

	public override void GenerateIntoMap(Map map)
	{
		if (Find.GameInitData == null)
		{
			return;
		}
		List<Thing> list = new List<Thing>();
		foreach (ScenPart allPart in Find.Scenario.AllParts)
		{
			list.AddRange(allPart.PlayerStartingThings());
		}
		foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
		{
			foreach (ThingDefCount item in Find.GameInitData.startingPossessions[startingAndOptionalPawn])
			{
				list.Add(StartingPawnUtility.GenerateStartingPossession(item));
			}
		}
		if (method == PlayerPawnsArriveMethod.Gravship && ModsConfig.OdysseyActive)
		{
			DoGravship(map, list);
		}
		else
		{
			DoDropPods(map, list);
		}
	}

	private void DoDropPods(Map map, List<Thing> startingItems)
	{
		List<List<Thing>> list = new List<List<Thing>>();
		foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
		{
			List<Thing> list2 = new List<Thing>();
			list2.Add(startingAndOptionalPawn);
			list.Add(list2);
		}
		int num = 0;
		foreach (Thing startingItem in startingItems)
		{
			if (startingItem.def.CanHaveFaction)
			{
				startingItem.SetFactionDirect(Faction.OfPlayer);
			}
			list[num].Add(startingItem);
			num++;
			if (num >= list.Count)
			{
				num = 0;
			}
		}
		DropPodUtility.DropThingGroupsNear(AreaFinder.FindNearEdgeCell(map, null), map, list, 110, Find.GameInitData.QuickStarted || method != PlayerPawnsArriveMethod.DropPods, leaveSlag: true, canRoofPunch: true, forbid: true, allowFogged: false);
	}

	private void DoGravship(Map map, List<Thing> startingItems)
	{
		Sketch sketch = SketchGen.Generate(parms: new SketchResolveParams
		{
			sketch = new Sketch()
		}, root: SketchResolverDefOf.Gravship);
		sketch.Rotate(Rot4.Random);
		HashSet<IntVec3> hashSet = sketch.OccupiedRect.Cells.Select((IntVec3 c) => c - sketch.OccupiedCenter).ToHashSet();
		List<CellRect> orGenerateVar = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");
		map.regionAndRoomUpdater.Enabled = true;
		IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;
		if (!MapGenerator.PlayerStartSpotValid)
		{
			GenStep_ReserveGravshipArea.SetStartSpot(map, hashSet, orGenerateVar);
			playerStartSpot = MapGenerator.PlayerStartSpot;
			GravshipPlacementUtility.ClearAreaForGravship(map, playerStartSpot, hashSet);
		}
		List<Thing> list = new List<Thing>();
		sketch.Spawn(map, playerStartSpot, Faction.OfPlayer, Sketch.SpawnPosType.OccupiedCenter, Sketch.SpawnMode.Normal, wipeIfCollides: true, forceTerrainAffordance: true, clearEdificeWhereFloor: true, list, dormant: false, buildRoofsInstantly: true);
		IntVec3 offset = playerStartSpot - sketch.OccupiedCenter;
		CellRect cellRect = sketch.OccupiedRect.MovedBy(offset);
		orGenerateVar.Add(cellRect);
		foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
		{
			if (!cellRect.TryRandomElement((IntVec3 c) => c.Standable(map) && (c.GetTerrain(map)?.IsSubstructure ?? false), out var result))
			{
				Log.Error("Could not find a valid spawn location for pawn " + startingAndOptionalPawn.Name);
			}
			else
			{
				GenPlace.TryPlaceThing(startingAndOptionalPawn, result, map, ThingPlaceMode.Near);
			}
		}
		foreach (Thing startingItem in startingItems)
		{
			if (startingItem.def.CanHaveFaction)
			{
				startingItem.SetFactionDirect(Faction.OfPlayer);
			}
			int num = startingItem.stackCount;
			int num2 = 99;
			while (num > 0 && num2-- > 0)
			{
				if (list.Where((Thing t) => t.def == ThingDefOf.Shelf || t.def == ThingDefOf.ShelfSmall).TryRandomElement(out var result2))
				{
					IntVec3 randomCell = result2.OccupiedRect().RandomCell;
					Thing thing = startingItem.SplitOff(Math.Min(startingItem.def.stackLimit, num));
					num -= thing.stackCount;
					GenPlace.TryPlaceThing(thing, randomCell, map, ThingPlaceMode.Near);
				}
			}
		}
		foreach (Thing item in list)
		{
			if (item.def == ThingDefOf.Door)
			{
				MapGenerator.rootsToUnfog.AddRange(GenAdj.CellsAdjacentCardinal(item));
			}
			if (item.TryGetComp(out CompRefuelable comp))
			{
				comp.Refuel(comp.Props.fuelCapacity);
			}
			if (item is Building_GravEngine building_GravEngine)
			{
				building_GravEngine.silentlyActivate = true;
			}
		}
	}
}
