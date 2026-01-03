using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_MechOperateDeepDrill : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOnBurningImmobile(TargetIndex.A);
		this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
		this.FailOn(() => !job.targetA.Thing.TryGetComp<CompDeepDrill>().CanDrillNow());
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
		Toil work = ToilMaker.MakeToil("MakeNewToils");
		work.tickAction = delegate
		{
			Pawn actor = work.actor;
			((Building)actor.CurJob.targetA.Thing).GetComp<CompDeepDrill>().DrillWorkDone(actor, 1);
		};
		work.defaultCompleteMode = ToilCompleteMode.Never;
		work.WithEffect(EffecterDefOf.Drill, TargetIndex.A);
		work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
		work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		work.activeSkill = () => SkillDefOf.Mining;
		yield return work;
	}
}
