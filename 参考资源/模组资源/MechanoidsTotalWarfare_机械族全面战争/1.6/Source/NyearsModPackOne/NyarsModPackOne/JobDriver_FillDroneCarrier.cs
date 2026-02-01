using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace NyarsModPackOne;

public class JobDriver_FillDroneCarrier : JobDriver
{
	private const TargetIndex CarrierIndex = TargetIndex.A;

	private const TargetIndex ResourceIndex = TargetIndex.B;

	protected Thing Carrier => job.GetTarget(TargetIndex.A).Thing;

	protected Thing Resource => job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Carrier, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Resource, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		yield return new Toil
		{
			initAction = delegate
			{
				pawn.pather.StopDead();
			},
			defaultCompleteMode = ToilCompleteMode.Delay,
			defaultDuration = 80
		};
		yield return new Toil
		{
			initAction = delegate
			{
				CompDroneCarrier compDroneCarrier = Carrier.TryGetComp<CompDroneCarrier>();
				if (compDroneCarrier != null)
				{
					int stackCount = Resource.stackCount;
					compDroneCarrier.AddIngredient(Resource.def, stackCount);
					if (pawn.carryTracker.CarriedThing == Resource)
					{
						pawn.carryTracker.DestroyCarriedThing();
					}
					else
					{
						Resource.Destroy();
					}
				}
			},
			defaultCompleteMode = ToilCompleteMode.Instant
		};
	}
}
