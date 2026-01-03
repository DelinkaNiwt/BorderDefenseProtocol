using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_GetDroneEnergy_SelfShutdown : JobGiver_GetEnergy
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		if (compDrone == null)
		{
			return null;
		}
		if (RCellFinder.TryFindRandomMechSelfShutdownSpot(pawn.Position, pawn, pawn.Map, out var result))
		{
			Job job = JobMaker.MakeJob(AncotJobDefOf.Ancot_DroneSelfShutdown, result);
			job.checkOverrideOnExpire = true;
			job.expiryInterval = 500;
			return job;
		}
		return null;
	}
}
