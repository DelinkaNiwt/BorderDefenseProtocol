using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace GD3
{
	public class JobGiver_WaitWhenBeaten : ThinkNode_JobGiver
	{
		private int overrideExpiryInterval = -1;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_WaitWhenBeaten obj = (JobGiver_WaitWhenBeaten)base.DeepCopy(resolve);
			obj.overrideExpiryInterval = overrideExpiryInterval;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			BlackApocriton blackApocriton = pawn as BlackApocriton;
			if (blackApocriton == null || !blackApocriton.beaten)
            {
				return null;
            }
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
			job.overrideFacing = Rot4.South;
			job.forceMaintainFacing = true;
			return job;
		}
	}
}