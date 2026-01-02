using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_HaulToOwnTransporter : JobDriver
{
	private Thing targetItem => job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(targetItem, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		pawn.TryGetComp<CompTransporterCustom>();
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.WaitWith(TargetIndex.A, 30, useProgressBar: true, maintainPosture: true, maintainSleep: false, TargetIndex.A);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		yield return toil;
		yield return Toils_General.Do(HaulToTransporter);
	}

	public virtual void HaulToTransporter()
	{
		Thing thing = job.GetTarget(TargetIndex.A).Thing;
		CompTransporterCustom compTransporterCustom = pawn.TryGetComp<CompTransporterCustom>();
		if (compTransporterCustom != null && compTransporterCustom.innerContainer.CanAcceptAnyOf(thing))
		{
			thing.DeSpawn();
			compTransporterCustom.innerContainer.TryAddOrTransfer(thing);
			compTransporterCustom.Notify_ThingAdded(thing);
			pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
		}
		else
		{
			Messages.Message("Ancot.TransporterNotAvailable".Translate(), MessageTypeDefOf.CautionInput);
			pawn.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
		}
	}
}
