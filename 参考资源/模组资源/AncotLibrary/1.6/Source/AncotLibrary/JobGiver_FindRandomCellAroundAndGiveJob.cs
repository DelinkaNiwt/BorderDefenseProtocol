using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_FindRandomCellAroundAndGiveJob : ThinkNode_JobGiver
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
			if (item.IsValid && item.InBounds(pawn.Map))
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
		JobGiver_FindRandomCellAroundAndGiveJob jobGiver_FindRandomCellAroundAndGiveJob = (JobGiver_FindRandomCellAroundAndGiveJob)base.DeepCopy(resolve);
		jobGiver_FindRandomCellAroundAndGiveJob.jobDef = jobDef;
		jobGiver_FindRandomCellAroundAndGiveJob.minDistance = minDistance;
		jobGiver_FindRandomCellAroundAndGiveJob.maxDistance = maxDistance;
		return jobGiver_FindRandomCellAroundAndGiveJob;
	}
}
