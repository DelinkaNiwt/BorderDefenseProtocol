using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace NCL;

internal class JobDriver_RebuildTheWarBeacon : JobDriver
{
	private const TargetIndex GraveIndex = TargetIndex.A;

	private const TargetIndex ThingIndex = TargetIndex.B;

	private const int Duration = 200;

	protected Building_Ancient_WarBeacon Grave => (Building_Ancient_WarBeacon)job.GetTarget(TargetIndex.A).Thing;

	protected Thing Thing => job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Grave, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Thing, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOnBurningImmobile(TargetIndex.A);
		AddEndCondition(() => (Grave.requiredThings.Any() && Grave.allowFilling) ? JobCondition.Ongoing : JobCondition.Succeeded);
		yield return Toils_General.DoAtomic(delegate
		{
			job.count = Grave.RequiredCountFor(Thing.def);
		});
		Toil reserveWort = Toils_Reserve.Reserve(TargetIndex.B);
		yield return reserveWort;
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
		yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveWort, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		yield return Toils_General.Wait(200).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
			.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
			.WithProgressBarToilDelay(TargetIndex.A);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			Grave.TryAcceptThing(Thing);
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil;
	}
}
