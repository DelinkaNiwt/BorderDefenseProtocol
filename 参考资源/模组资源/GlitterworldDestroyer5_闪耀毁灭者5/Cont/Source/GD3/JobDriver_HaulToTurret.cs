using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
	public class JobDriver_HaulToTurret : JobDriver
	{
		protected Thing Refuelable => job.GetTarget(TargetIndex.A).Thing;

		protected CompDeckReinforce RefuelableComp => Refuelable.TryGetComp<CompDeckReinforce>();

		protected Thing Fuel => job.GetTarget(TargetIndex.B).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (pawn.Reserve(Refuelable, job, 1, -1, null, errorOnFailed))
			{
				return pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed);
			}
			return false;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			AddEndCondition(() => (!RefuelableComp.IsFull) ? JobCondition.Ongoing : JobCondition.Succeeded);
			AddFailCondition(() => RefuelableComp.IsFull || !RefuelableComp.shouldBeNoticed);
			yield return Toils_General.DoAtomic(delegate
			{
				job.count = Mathf.Max(Mathf.CeilToInt(RefuelableComp.CostNow - RefuelableComp.decks), 1);
			});
			Toil reserveFuel = Toils_Reserve.Reserve(TargetIndex.B);
			yield return reserveFuel;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(120).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
				.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
				.WithProgressBarToilDelay(TargetIndex.A);
			Toil toil = ToilMaker.MakeToil("FinalizeRefueling");
			toil.initAction = delegate
			{
				Job curJob = toil.actor.CurJob;
				Thing thing = curJob.GetTarget(TargetIndex.A).Thing;
				if (toil.actor.CurJob.placedThings.NullOrEmpty())
				{
					thing.TryGetComp<CompDeckReinforce>().Refuel(new List<Thing> { curJob.GetTarget(TargetIndex.B).Thing });
				}
				else
				{
					thing.TryGetComp<CompDeckReinforce>().Refuel(toil.actor.CurJob.placedThings.Select((ThingCountClass p) => p.thing).ToList());
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return toil;
		}
	}

}
