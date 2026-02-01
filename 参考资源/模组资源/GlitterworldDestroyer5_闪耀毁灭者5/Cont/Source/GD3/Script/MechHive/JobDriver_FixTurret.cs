using System;
using Verse;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;

namespace GD3
{
	public class JobDriver_FixTurret : JobDriver
	{

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public Building_BrokenTurret Turret => job.targetA.Thing as Building_BrokenTurret;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.A);
			this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
			this.FailOn(() => !Turret.shouldBeNoticed);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil work = ToilMaker.MakeToil("MakeNewToils");
			work.tickAction = delegate
			{
				Pawn actor = work.actor;
				((Building_BrokenTurret)actor.CurJob.targetA.Thing).WorkOn(pawn);
				actor.skills?.Learn(SkillDefOf.Construction, 0.065f);
			};
			work.defaultCompleteMode = ToilCompleteMode.Never;
			work.WithProgressBar(TargetIndex.A, () => Turret.WorkProgress);
			work.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
			work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			work.activeSkill = () => SkillDefOf.Construction;
			yield return work;
		}
	}
}