using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace GD3
{
	public class JobGiver_ObserverFollow : JobGiver_AIFollowPawn
	{
		protected override int FollowJobExpireInterval
		{
			get
			{
				return 80;
			}
		}

		protected override Pawn GetFollowee(Pawn pawn)
		{
			CompObserverLink comp = pawn.TryGetComp<CompObserverLink>();
			if (comp == null || comp.tmpPawns.Count == 0)
			{
				return null;
			}
			return comp.tmpPawns[0];
		}

		protected override float GetRadius(Pawn pawn)
		{
			return 2.9f;
		}

		protected override Job TryGiveJob(Pawn pawn)
        {
            CompObserverLink comp = pawn.TryGetComp<CompObserverLink>();
            if (comp == null || comp.tmpPawns.Count == 0 || pawn.CurJobDef == JobDefOf.MechCharge)
			{
				return null;
			}
			return base.TryGiveJob(pawn);
		}
	}
}
