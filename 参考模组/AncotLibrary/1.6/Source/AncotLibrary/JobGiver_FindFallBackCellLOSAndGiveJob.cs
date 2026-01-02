using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_FindFallBackCellLOSAndGiveJob : ThinkNode_JobGiver
{
	public JobDef jobDef;

	public float minDistance = 10f;

	public float maxDistance = 20f;

	private static List<Thing> tmpHostileSpots = new List<Thing>();

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Downed || pawn.Dead)
		{
			return null;
		}
		if (GetDestination(pawn, out var relocatePosition))
		{
			return JobMaker.MakeJob(jobDef, new LocalTargetInfo(relocatePosition));
		}
		return null;
	}

	private bool GetDestination(Pawn pawn, out IntVec3 relocatePosition)
	{
		Map map = pawn.Map;
		tmpHostileSpots.Clear();
		tmpHostileSpots.AddRange(from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
			where !a.ThreatDisabled(pawn)
			select a.Thing);
		float num = Rand.Range(maxDistance, minDistance);
		relocatePosition = CellFinderLoose.GetFallbackDest(pawn, tmpHostileSpots, num, 5f, 5f, 20, (IntVec3 c) => c.IsValid && c.Walkable(map) && c.InBounds(map));
		tmpHostileSpots.Clear();
		return relocatePosition.IsValid;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_FindFallBackCellLOSAndGiveJob jobGiver_FindFallBackCellLOSAndGiveJob = (JobGiver_FindFallBackCellLOSAndGiveJob)base.DeepCopy(resolve);
		jobGiver_FindFallBackCellLOSAndGiveJob.jobDef = jobDef;
		jobGiver_FindFallBackCellLOSAndGiveJob.minDistance = minDistance;
		jobGiver_FindFallBackCellLOSAndGiveJob.maxDistance = maxDistance;
		return jobGiver_FindFallBackCellLOSAndGiveJob;
	}
}
