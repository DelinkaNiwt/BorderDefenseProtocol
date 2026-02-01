using Verse;
using Verse.AI;

namespace NCL;

public class JobGiver_AttackMinitor : ThinkNode_JobGiver
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		Thing target = FindClosestMinitor(pawn);
		if (target == null)
		{
			return null;
		}
		return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("AttackUniversityMinitor"), target, 600, checkOverrideOnExpiry: true);
	}

	private Thing FindClosestMinitor(Pawn pawn)
	{
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("TW_University_Minitor")), PathEndMode.Touch, TraverseParms.For(pawn), 50f, (Thing t) => t is Pawn { Downed: false, Dead: false } pawn2 && !pawn.Downed && !pawn.Dead && !pawn2.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("TW_WasEncouraged")));
	}
}
