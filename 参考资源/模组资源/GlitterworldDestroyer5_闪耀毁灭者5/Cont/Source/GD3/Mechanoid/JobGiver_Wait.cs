using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace GD3
{
	public class JobGiver_Wait : ThinkNode_JobGiver
	{
		private int overrideExpiryInterval = -1;

		private bool checkFlying = true;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_Wait obj = (JobGiver_Wait)base.DeepCopy(resolve);
			obj.overrideExpiryInterval = overrideExpiryInterval;
			obj.checkFlying = checkFlying;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, pawn.PositionHeld);
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
			job.overrideFacing = pawn.Rotation;
			job.forceMaintainFacing = true;
			return job;
		}
	}
}
