using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_DroneSelfShutdown : ThinkNode_JobGiver
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		if (RCellFinder.TryFindNearbyMechSelfShutdownSpot(pawn.Position, pawn, pawn.Map, out var result, allowForbidden: true))
		{
			Job job = JobMaker.MakeJob(AncotJobDefOf.Ancot_DroneSelfShutdown, result);
			job.forceSleep = true;
			return job;
		}
		return null;
	}
}
