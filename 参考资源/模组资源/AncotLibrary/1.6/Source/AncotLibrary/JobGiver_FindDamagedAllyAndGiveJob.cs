using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_FindDamagedAllyAndGiveJob : ThinkNode_JobGiver
{
	public JobDef jobDef;

	public float minDistance = 0f;

	public float maxDistance = 20f;

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Dead || pawn.Downed)
		{
			return null;
		}
		if (TryFindWeakestAlly(pawn, out var ally))
		{
			return JobMaker.MakeJob(jobDef, new LocalTargetInfo(ally));
		}
		return null;
	}

	private bool TryFindWeakestAlly(Pawn pawn, out Pawn ally)
	{
		ally = null;
		float num = 1f;
		List<Pawn> list = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
		foreach (Pawn item in list)
		{
			if (item == pawn || item.Dead || item.Downed || item.health == null)
			{
				continue;
			}
			float lengthHorizontal = (item.Position - pawn.Position).LengthHorizontal;
			if (!(lengthHorizontal < minDistance) && !(lengthHorizontal > maxDistance))
			{
				float summaryHealthPercent = item.health.summaryHealth.SummaryHealthPercent;
				if (summaryHealthPercent < num)
				{
					num = summaryHealthPercent;
					ally = item;
				}
			}
		}
		return ally != null;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_FindDamagedAllyAndGiveJob jobGiver_FindDamagedAllyAndGiveJob = (JobGiver_FindDamagedAllyAndGiveJob)base.DeepCopy(resolve);
		jobGiver_FindDamagedAllyAndGiveJob.jobDef = jobDef;
		jobGiver_FindDamagedAllyAndGiveJob.minDistance = minDistance;
		jobGiver_FindDamagedAllyAndGiveJob.maxDistance = maxDistance;
		return jobGiver_FindDamagedAllyAndGiveJob;
	}
}
