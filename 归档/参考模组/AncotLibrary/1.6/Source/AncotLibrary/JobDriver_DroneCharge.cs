using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_DroneCharge : JobDriver
{
	private const TargetIndex ChargerInd = TargetIndex.A;

	public Building_DroneCharger Charger => (Building_DroneCharger)job.targetA.Thing;

	private CompDrone CompDrone => pawn.TryGetComp<CompDrone>();

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (!pawn.Reserve(Charger, job, 1, -1, null, errorOnFailed))
		{
			return false;
		}
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		this.FailOn(() => !Charger.CanPawnChargeCurrently(pawn));
		yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell).FailOnForbidden(TargetIndex.A);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		toil.initAction = delegate
		{
			Charger.StartCharging(pawn);
		};
		toil.AddFinishAction(delegate
		{
			if (Charger.CurrentlyChargingMech == pawn)
			{
				Charger.StopCharging();
			}
		});
		toil.handlingFacing = true;
		toil.tickIntervalAction = (Action<int>)Delegate.Combine(toil.tickIntervalAction, (Action<int>)delegate
		{
			pawn.rotationTracker.FaceTarget(Charger.Position);
			if (CompDrone.PercentFull >= 1f)
			{
				ReadyForNextToil();
			}
		});
		yield return toil;
	}
}
