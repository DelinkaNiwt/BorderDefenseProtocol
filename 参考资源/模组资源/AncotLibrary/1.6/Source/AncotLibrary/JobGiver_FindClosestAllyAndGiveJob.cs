using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_FindClosestAllyAndGiveJob : ThinkNode_JobGiver
{
	public JobDef jobDef;

	public float minDistance = 0f;

	public float maxDistance = 20f;

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Downed || pawn.Dead)
		{
			return null;
		}
		if (TryFindClosestAlly(pawn, out var closestAlly))
		{
			return JobMaker.MakeJob(jobDef, new LocalTargetInfo(closestAlly));
		}
		return null;
	}

	private bool TryFindClosestAlly(Pawn pawn, out Pawn closestAlly)
	{
		closestAlly = null;
		float num = float.MaxValue;
		List<Pawn> list = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
		foreach (Pawn item in list)
		{
			if (item != pawn && !item.Dead && !item.Downed)
			{
				float num2 = (item.Position - pawn.Position).LengthHorizontalSquared;
				float num3 = Mathf.Sqrt(num2);
				if (num3 >= minDistance && num3 <= maxDistance && num2 < num)
				{
					num = num2;
					closestAlly = item;
				}
			}
		}
		return closestAlly != null;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_FindClosestAllyAndGiveJob jobGiver_FindClosestAllyAndGiveJob = (JobGiver_FindClosestAllyAndGiveJob)base.DeepCopy(resolve);
		jobGiver_FindClosestAllyAndGiveJob.jobDef = jobDef;
		jobGiver_FindClosestAllyAndGiveJob.minDistance = minDistance;
		jobGiver_FindClosestAllyAndGiveJob.maxDistance = maxDistance;
		return jobGiver_FindClosestAllyAndGiveJob;
	}
}
