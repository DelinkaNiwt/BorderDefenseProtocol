using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCLWorm;

public class Comp_NCLPush : CompAbilityEffect
{
	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (target.Thing is Pawn { Map: var map } pawn)
		{
			if (TryFindRandomStandableEdgeCell(map, out var result))
			{
				FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipFlashEntry);
				pawn.DeSpawn(DestroyMode.WillReplace);
				GenSpawn.Spawn(pawn, result, map);
				FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipInnerExit);
			}
			else
			{
				Log.Message("Could not find valid edge cell.");
			}
		}
	}

	private static bool TryFindRandomStandableEdgeCell(Map map, out IntVec3 result, int maxAttempts = 100)
	{
		int num = 5;
		for (int i = 0; i < maxAttempts; i++)
		{
			IntVec3 intVec = CellFinder.RandomEdgeCell(map);
			IntVec3 intVec2 = map.Center - intVec;
			float num2 = Mathf.Sqrt(intVec2.x * intVec2.x + intVec2.z * intVec2.z);
			IntVec3 intVec3 = ((num2 > 0.01f) ? new IntVec3(Mathf.RoundToInt((float)intVec2.x / num2), 0, Mathf.RoundToInt((float)intVec2.z / num2)) : new IntVec3(0, 0, 0));
			IntVec3 intVec4 = intVec + intVec3 * num;
			if (Rand.Chance(0.7f))
			{
				intVec4 += new IntVec3(Rand.RangeInclusive(-2, 2), 0, Rand.RangeInclusive(-2, 2));
			}
			if (IsValidTeleportSpot(intVec4, map))
			{
				result = intVec4;
				return true;
			}
		}
		Predicate<IntVec3> validator = (IntVec3 c) => IsValidTeleportSpot(c, map);
		if (CellFinder.TryFindRandomCell(map, validator, out result))
		{
			return true;
		}
		result = IntVec3.Invalid;
		Log.Warning("[NCL] Comp_NCLPush could not find a standable edge cell after " + maxAttempts + " attempts.");
		return false;
	}

	private static int DistanceToEdge(IntVec3 cell, Map map)
	{
		if (!cell.InBounds(map))
		{
			return 0;
		}
		int b = Mathf.Min(cell.x, map.Size.x - cell.x - 1, cell.z, map.Size.z - cell.z - 1);
		return Mathf.Max(0, b);
	}

	private static bool IsValidTeleportSpot(IntVec3 candidate, Map map)
	{
		if (!candidate.InBounds(map) || !candidate.Standable(map) || DistanceToEdge(candidate, map) < 5)
		{
			return false;
		}
		if (map.fogGrid != null && map.fogGrid.IsFogged(candidate))
		{
			return false;
		}
		Building edifice = candidate.GetEdifice(map);
		if (edifice != null && (edifice.def.passability == Traversability.Impassable || edifice.def.IsDoor))
		{
			return false;
		}
		if (map.roofGrid.Roofed(candidate) && map.roofGrid.RoofAt(candidate).isThickRoof)
		{
			return false;
		}
		return true;
	}
}
