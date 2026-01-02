using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_DestroyItem : JobDriver
{
	public Thing TargetItem => job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(TargetItem, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.WaitWith(TargetIndex.A, 30, useProgressBar: true, maintainPosture: true, maintainSleep: false, TargetIndex.A);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		yield return toil;
		yield return Toils_General.Do(Destroy);
	}

	public virtual void Destroy()
	{
		TargetItem.Destroy();
	}
}
