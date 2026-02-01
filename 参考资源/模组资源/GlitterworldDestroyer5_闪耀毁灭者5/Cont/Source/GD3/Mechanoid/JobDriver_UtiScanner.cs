using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace GD3
{
	public class JobDriver_UtiScanner : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			CompScanner scannerComp = this.job.targetA.Thing.TryGetComp<CompScanner>();
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.A);
			this.FailOn(() => !scannerComp.CanUseNow);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			Toil work = ToilMaker.MakeToil("MakeNewToils");
			work.tickIntervalAction = delegate (int delta)
			{
				Pawn actor = work.actor;
				Building building = (Building)actor.CurJob.targetA.Thing;
				scannerComp.Used(actor);
				if (!this.pawn.RaceProps.IsMechanoid)
				{
					actor.skills.Learn(SkillDefOf.Intellectual, 0.035f, false);
					actor.GainComfortFromCellIfPossible(delta, true);
				}
			};
			work.PlaySustainerOrSound(scannerComp.Props.soundWorking, 1f);
			work.AddFailCondition(() => !scannerComp.CanUseNow);
			work.defaultCompleteMode = ToilCompleteMode.Never;
			work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			work.activeSkill = (() => SkillDefOf.Intellectual);
			yield return work;
			yield break;
		}
	}
}
