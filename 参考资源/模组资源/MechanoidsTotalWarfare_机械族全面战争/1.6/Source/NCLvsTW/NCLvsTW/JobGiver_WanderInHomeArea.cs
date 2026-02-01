using System.Linq;
using Verse;
using Verse.AI;

namespace NCLvsTW;

public class JobGiver_WanderInHomeArea : JobGiver_Wander
{
	public JobGiver_WanderInHomeArea()
	{
		wanderRadius = 7.5f;
		ticksBetweenWandersRange = new IntRange(125, 200);
	}

	protected override IntVec3 GetWanderRoot(Pawn pawn)
	{
		if (pawn.Map.areaManager.Home.ActiveCells.TryRandomElement(out var result))
		{
			return result;
		}
		return pawn.Position;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Map.areaManager.Home.ActiveCells.Where((IntVec3 cell) => pawn.CanReach(cell, PathEndMode.OnCell, Danger.None)).TryRandomElement(out var _))
		{
			return base.TryGiveJob(pawn);
		}
		return null;
	}
}
