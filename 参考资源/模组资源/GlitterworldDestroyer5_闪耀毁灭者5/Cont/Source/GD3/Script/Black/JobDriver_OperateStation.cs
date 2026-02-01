using System;
using Verse;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;

namespace GD3
{
    public class JobDriver_OperateStation : JobDriver
    {

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public CompCommunicationStation Comp => ((Building)job.targetA.Thing).TryGetComp<CompCommunicationStation>();

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.A);
			this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
			this.FailOn(() => !Comp.CanOperateNow());
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			Toil work = ToilMaker.MakeToil("MakeNewToils");
			work.tickAction = delegate
			{
				Pawn actor = work.actor;
				((Building)actor.CurJob.targetA.Thing).GetComp<CompCommunicationStation>().OperateWorkDone(actor);
				actor.skills.Learn(SkillDefOf.Intellectual, 0.065f);
			};
			work.defaultCompleteMode = ToilCompleteMode.Never;
			work.WithProgressBar(TargetIndex.A, () => Comp.Percent);
			work.WithEffect(EffecterDefOf.Research, TargetIndex.A);
			work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			work.activeSkill = () => SkillDefOf.Intellectual;
			yield return work;
		}
	}
}
