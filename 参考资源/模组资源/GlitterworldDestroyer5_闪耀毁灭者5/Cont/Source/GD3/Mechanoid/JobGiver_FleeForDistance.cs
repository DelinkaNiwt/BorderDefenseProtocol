using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace GD3
{
	public class JobGiver_FleeForDistance : ThinkNode_JobGiver
	{
		protected float enemyDistToFleeRange = 7.9f;

		protected FloatRange fleeDistRange = new FloatRange(13.5f, 20f);

		private List<Thing> tmpThings = new List<Thing>();

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (GenAI.EnemyIsNear(pawn, enemyDistToFleeRange, out var threat, meleeOnly: false, requireLos: true))
			{
				return FleeJob(pawn, threat, Mathf.CeilToInt(fleeDistRange.RandomInRange));
			}
			return null;
		}

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_FleeForDistance obj = (JobGiver_FleeForDistance)base.DeepCopy(resolve);
			obj.enemyDistToFleeRange = enemyDistToFleeRange;
			obj.fleeDistRange = fleeDistRange;
			return obj;
		}

		public Job FleeJob(Pawn pawn, Thing danger, int fleeDistance)
		{
			IntVec3 intVec;
			if (pawn.CurJob != null && pawn.CurJob.def == GDDefOf.GD_FleeFlying)
			{
				intVec = pawn.CurJob.targetA.Cell;
			}
			else
			{
				tmpThings.Clear();
				tmpThings.Add(danger);
				intVec = CellFinderLoose.GetFleeDest(pawn, tmpThings, fleeDistance);
				tmpThings.Clear();
			}

			if (intVec != pawn.Position)
			{
				return JobMaker.MakeJob(GDDefOf.GD_FleeFlying, intVec, danger);
			}

			return null;
		}
	}
}
