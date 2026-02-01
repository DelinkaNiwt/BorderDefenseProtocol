using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace NyarsModPackOne;

public class JobDriver_DroneSlefDetonate : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetA.Cell);
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		Toil f = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		yield return f.FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			((Drone)pawn).activeExplosion = true;
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil;
	}
}
