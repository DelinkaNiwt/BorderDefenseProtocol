using System;
using Verse;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;

namespace GD3
{
	public class JobDriver_UpgradeTurret : JobDriver
	{

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public CompDeckReinforce Comp => ((Building)job.targetA.Thing).TryGetComp<CompDeckReinforce>();

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.A);
			this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
			this.FailOn(() => !Comp.shouldBeNoticed);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil work = ToilMaker.MakeToil("MakeNewToils");
			work.tickAction = delegate
			{
				Pawn actor = work.actor;
				((Building)actor.CurJob.targetA.Thing).GetComp<CompDeckReinforce>().WorkOn(pawn);
				actor.skills?.Learn(SkillDefOf.Construction, 0.065f);
			};
			work.defaultCompleteMode = ToilCompleteMode.Never;
			work.WithProgressBar(TargetIndex.A, () => Comp.WorkProgress);
			work.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
			work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			work.activeSkill = () => SkillDefOf.Construction;
			yield return work;
		}
	}
}

