using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_HaulDroneToCharger : JobDriver
{
	private const TargetIndex MechInd = TargetIndex.A;

	private const TargetIndex ChargerInd = TargetIndex.B;

	private const TargetIndex ChargerCellInd = TargetIndex.C;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed))
		{
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.A);
		yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C, PathEndMode.OnCell);
		yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, null, storageMode: false);
		yield return Toils_General.Do(delegate
		{
			base.pawn.Map.reservationManager.Release(job.targetB, base.pawn, job);
			Pawn pawn = (Pawn)job.targetA.Thing;
			LocalTargetInfo targetA = (Building_DroneCharger)job.targetB.Thing;
			Job newJob = JobMaker.MakeJob(AncotJobDefOf.Ancot_DroneCharge, targetA);
			pawn.jobs.StartJob(newJob, JobCondition.InterruptForced);
		});
	}
}
