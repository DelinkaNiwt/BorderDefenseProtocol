using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_ReleaseMechs_Custom : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			pawn.TryGetComp<CompMechCarrier_Custom>().TrySpawnPawns();
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil;
	}
}
