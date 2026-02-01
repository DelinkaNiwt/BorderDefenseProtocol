using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace GD3
{
	public class JobGiver_WaitWithoutEnemies : ThinkNode_JobGiver
	{
		private int overrideExpiryInterval = -1;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_WaitWithoutEnemies obj = (JobGiver_WaitWithoutEnemies)base.DeepCopy(resolve);
			obj.overrideExpiryInterval = overrideExpiryInterval;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			bool flag = pawn.TryGetComp<CompArchoDrone>()?.alert ?? true;
			if (!flag)
            {
				Job job = JobMaker.MakeJob(JobDefOf.Wait, pawn.PositionHeld);
				if (overrideExpiryInterval > 0)
				{
					job.expiryInterval = overrideExpiryInterval;
				}
				else
				{
					job.intervalScalingTarget = TargetIndex.A;
				}
				job.checkOverrideOnExpire = true;
				job.expireRequiresEnemiesNearby = true;
				job.collideWithPawns = true;
				job.forceMaintainFacing = true;
				return job;
			}
			return null;
		}
	}
}
