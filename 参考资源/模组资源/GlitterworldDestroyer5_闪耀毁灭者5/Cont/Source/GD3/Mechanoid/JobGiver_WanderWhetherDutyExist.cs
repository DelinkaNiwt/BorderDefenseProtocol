using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class JobGiver_WanderWhetherDutyExist : JobGiver_Wander
	{
		public JobGiver_WanderWhetherDutyExist()
		{
			wanderRadius = 7f;
			ticksBetweenWandersRange = new IntRange(125, 200);
		}

		protected override IntVec3 GetWanderRoot(Pawn pawn)
		{
			return pawn.mindState.duty?.focus != null ? WanderUtility.BestCloseWanderRoot(pawn.mindState.duty.focus.Cell, pawn) : pawn.Position;
		}
	}

}
