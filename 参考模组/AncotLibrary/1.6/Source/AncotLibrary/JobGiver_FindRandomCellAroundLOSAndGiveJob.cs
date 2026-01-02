using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_FindRandomCellAroundLOSAndGiveJob : ThinkNode_JobGiver
{
	public JobDef jobDef;

	public float minDistance = 10f;

	public float maxDistance = 20f;

	private static List<IntVec3> tmpCells = new List<IntVec3>();

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Downed || pawn.Dead)
		{
			return null;
		}
		if (GetDestination(pawn, out var targetCell))
		{
			return JobMaker.MakeJob(jobDef, new LocalTargetInfo(targetCell));
		}
		return null;
	}

	private bool GetDestination(Pawn pawn, out IntVec3 targetCell)
	{
		tmpCells.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(pawn.Position, maxDistance, useCenter: true))
		{
			if (item.IsValid && item.InBounds(pawn.Map) && GenSight.LineOfSight(pawn.Position, item, pawn.Map, skipFirstCell: true))
			{
				tmpCells.Add(item);
			}
		}
		foreach (IntVec3 item2 in GenRadial.RadialCellsAround(pawn.Position, minDistance, useCenter: true))
		{
			if (tmpCells.Contains(item2))
			{
				tmpCells.Remove(item2);
			}
		}
		targetCell = tmpCells.RandomElementWithFallback();
		return targetCell.IsValid;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_FindRandomCellAroundLOSAndGiveJob jobGiver_FindRandomCellAroundLOSAndGiveJob = (JobGiver_FindRandomCellAroundLOSAndGiveJob)base.DeepCopy(resolve);
		jobGiver_FindRandomCellAroundLOSAndGiveJob.jobDef = jobDef;
		jobGiver_FindRandomCellAroundLOSAndGiveJob.minDistance = minDistance;
		jobGiver_FindRandomCellAroundLOSAndGiveJob.maxDistance = maxDistance;
		return jobGiver_FindRandomCellAroundLOSAndGiveJob;
	}
}
