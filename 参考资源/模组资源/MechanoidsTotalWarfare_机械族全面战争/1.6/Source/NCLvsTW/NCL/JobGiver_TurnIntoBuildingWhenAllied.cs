using Verse;
using Verse.AI;

namespace NCL;

public class JobGiver_TurnIntoBuildingWhenAllied : ThinkNode_JobGiver
{
	public float allySearchRadius = 15f;

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Faction == null)
		{
			return null;
		}
		if (pawn.abilities?.GetAbility(NCLContainerDefOf.TurnIntoBuildingAbility) == null)
		{
			return null;
		}
		bool hasAlly = false;
		foreach (Pawn other in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
		{
			if (other == null || other == pawn || other.Dead || !(other.Position.DistanceTo(pawn.Position) <= allySearchRadius))
			{
				continue;
			}
			hasAlly = true;
			break;
		}
		if (!hasAlly)
		{
			return null;
		}
		return JobMaker.MakeJob(NCLContainerDefOf.TurnIntoBuilding, pawn);
	}
}
