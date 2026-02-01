using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCLWorm;

public class Comp_TeleportToRandomAlly : CompAbilityEffect
{
	private const float MinDistance = 5f;

	private const float MaxDistance = 10f;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (!(target.Thing is Pawn { Map: not null } pawn))
		{
			return;
		}
		Pawn pawn2 = FindRandomEligibleAlly(pawn);
		if (pawn2 == null)
		{
			ExecuteTeleportSequence(pawn, parent.pawn.Position);
			return;
		}
		IntVec3 targetCell = FindSafeTeleportSpot(pawn2.Position, pawn.Map);
		if (!targetCell.IsValid)
		{
			targetCell = pawn2.Position;
		}
		ExecuteTeleportSequence(pawn, targetCell);
	}

	private Pawn FindRandomEligibleAlly(Pawn victim)
	{
		Faction faction = parent.pawn.Faction;
		return victim.Map.mapPawns.AllPawnsSpawned.Where((Pawn p) => p.Faction == faction && p != victim && p != parent.pawn && !p.Downed && !p.Dead && p.Spawned).RandomElementWithFallback();
	}

	private IntVec3 FindSafeTeleportSpot(IntVec3 center, Map map)
	{
		return CellFinder.RandomClosewalkCellNear(center, map, Mathf.RoundToInt(10f), (IntVec3 cell) => cell.DistanceTo(center) >= 5f && cell.Standable(map) && !cell.Fogged(map) && map.reachability.CanReach(center, cell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.NoPassClosedDoors)));
	}

	private void ExecuteTeleportSequence(Pawn victim, IntVec3 targetCell)
	{
		Map map = victim.Map;
		FleckMaker.Static(victim.Position, map, FleckDefOf.PsycastSkipFlashEntry);
		victim.DeSpawn(DestroyMode.WillReplace);
		GenSpawn.Spawn(victim, targetCell, map);
		FleckMaker.Static(targetCell, map, FleckDefOf.PsycastSkipInnerExit);
	}
}
