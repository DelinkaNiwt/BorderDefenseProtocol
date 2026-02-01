using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCLWorm;

public class Comp_ChaosTeleport : CompAbilityEffect
{
	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		Map map = parent.pawn.Map;
		List<Pawn> list = new List<Pawn>();
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (item.HostileTo(parent.pawn) || (parent.pawn.Faction != null && item.Faction != null && parent.pawn.Faction.HostileTo(item.Faction)))
			{
				list.Add(item);
			}
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			FleckMaker.Static(list[num].Position, map, FleckDefOf.PsycastSkipFlashEntry);
			list[num].DeSpawn(DestroyMode.WillReplace);
			IntVec3 loc = FindMapEdgeSafeSpot(list[num], map);
			GenSpawn.Spawn(list[num], loc, map);
			FleckMaker.Static(list[num].Position, map, FleckDefOf.PsycastSkipInnerExit);
		}
	}

	private IntVec3 FindMapEdgeSafeSpot(Pawn pawn, Map map)
	{
		Predicate<IntVec3> predicate = (IntVec3 cell) => IsSafeSpot(cell, map) && IsInEdgeBufferZone(cell, map);
		for (int num = 0; num < 100; num++)
		{
			IntVec3 edgeCell = CellFinder.RandomEdgeCell(map);
			int offset = Rand.Range(5, 11);
			IntVec3 offsetPosition = GetOffsetPosition(edgeCell, map, offset);
			if (offsetPosition.InBounds(map) && predicate(offsetPosition))
			{
				return offsetPosition;
			}
		}
		IntVec3 root = CellFinder.RandomEdgeCell(map);
		if (CellFinder.TryFindRandomCellNear(root, map, 15, predicate, out var result))
		{
			return result;
		}
		List<IntVec3> list = new List<IntVec3>
		{
			new IntVec3(5, 0, 5),
			new IntVec3(map.Size.x - 6, 0, 5),
			new IntVec3(5, 0, map.Size.z - 6),
			new IntVec3(map.Size.x - 6, 0, map.Size.z - 6)
		};
		foreach (IntVec3 item in list)
		{
			if (predicate(item))
			{
				return item;
			}
			if (CellFinder.TryFindRandomCellNear(item, map, 10, predicate, out result))
			{
				return result;
			}
		}
		return CellFinder.RandomEdgeCell(map);
	}

	private IntVec3 GetOffsetPosition(IntVec3 edgeCell, Map map, int offset)
	{
		offset = Mathf.Clamp(offset, 0, Mathf.Max(map.Size.x, map.Size.z) / 2);
		if (edgeCell.x == 0)
		{
			return new IntVec3(edgeCell.x + offset, 0, edgeCell.z);
		}
		if (edgeCell.x == map.Size.x - 1)
		{
			return new IntVec3(edgeCell.x - offset, 0, edgeCell.z);
		}
		if (edgeCell.z == 0)
		{
			return new IntVec3(edgeCell.x, 0, edgeCell.z + offset);
		}
		if (edgeCell.z == map.Size.z - 1)
		{
			return new IntVec3(edgeCell.x, 0, edgeCell.z - offset);
		}
		return edgeCell;
	}

	private bool IsInEdgeBufferZone(IntVec3 cell, Map map)
	{
		int x = cell.x;
		int num = map.Size.x - cell.x - 1;
		int z = cell.z;
		int num2 = map.Size.z - cell.z - 1;
		int num3 = Mathf.Min(x, num, z, num2);
		return num3 >= 5 && num3 <= 10;
	}

	private bool IsSafeSpot(IntVec3 cell, Map map)
	{
		if (!cell.InBounds(map))
		{
			return false;
		}
		FogGrid fogGrid = map.fogGrid;
		if (fogGrid != null && fogGrid.IsFogged(cell))
		{
			return false;
		}
		Building edifice = cell.GetEdifice(map);
		if (edifice != null && (edifice.def.passability == Traversability.Impassable || edifice.def.IsDoor))
		{
			return false;
		}
		TerrainDef terrain = cell.GetTerrain(map);
		if (terrain == null || terrain.passability == Traversability.Impassable)
		{
			return false;
		}
		if (terrain.IsWater && terrain.extraNonDraftedPerceivedPathCost > 30)
		{
			return false;
		}
		if (map.roofGrid.Roofed(cell) && map.roofGrid.RoofAt(cell).isThickRoof)
		{
			return false;
		}
		return cell.Standable(map);
	}
}
