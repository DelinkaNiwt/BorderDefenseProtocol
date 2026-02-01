using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public abstract class JobDriver_InteractBase : JobDriver
	{
		public Thing InteractableThing => job.GetTarget(TargetIndex.A).Thing;

		public virtual Thing OptionalThing => job.GetTarget(TargetIndex.B).Thing;

		public bool HasOptionalThing => OptionalThing != null;

		public abstract int InteractingTicks { get; }

		public abstract bool FailWhen { get; }

		public virtual bool HasInteractionCell => false;

		public virtual EffecterDef Effecter => null;

		public virtual SoundDef Sound => null;

		public virtual bool DestroyThingB => false;


		private float progress;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (base.TargetThingB != null)
			{
				pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
			}
			if (base.TargetThingB != null && !pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (job.GetTarget(TargetIndex.B).HasThing)
			{
				Toil opt = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, canGotoSpawnedParent: true).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B)
					.FailOn(() => FailWhen);
				yield return opt;
				yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true, failIfStackCountLessThanJobCount: false, reserve: true, canTakeFromInventory: true);
				yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(opt, TargetIndex.B);
			}
			if (job.GetTarget(TargetIndex.A).Thing.SpawnedParentOrMe != null)
			{
				if (HasInteractionCell)
				{
					job.SetTarget(TargetIndex.C, job.GetTarget(TargetIndex.A).Thing.SpawnedParentOrMe.InteractionCell);
				}
				else
				{
					job.SetTarget(TargetIndex.C, job.GetTarget(TargetIndex.A).Thing.SpawnedParentOrMe);
				}
			}
			PathEndMode mode = HasInteractionCell ? PathEndMode.OnCell : PathEndMode.Touch;
			yield return Toils_Goto.GotoThing(TargetIndex.C, mode).FailOnDespawnedNullOrForbidden(TargetIndex.C).FailOnSomeonePhysicallyInteracting(TargetIndex.C)
				.FailOn(() => FailWhen);
			int num = InteractingTicks;
			int remainingTicks = Mathf.RoundToInt((float)num * (1f - progress));
			yield return WaitForActivate(remainingTicks, num);
			yield return Toils_General.Do(delegate
			{
				Action();
				if (HasOptionalThing)
				{
					if (pawn.IsCarryingThing(OptionalThing))
					{
						if (DestroyThingB)
						{
							pawn.carryTracker.DestroyCarriedThing();
						}
					}
				}
			});
		}

		public abstract void Action();

		public virtual void TickAction()
		{

		}

		public virtual void FinishAction()
        {

        }

		public Toil WaitForActivate(int remainingTicks, int totalTicks)
		{
			Toil toil = ToilMaker.MakeToil("WaitForActivate").FailOn(() => FailWhen);
			toil.WithProgressBar(TargetIndex.A, () => progress);
			if (Effecter != null)
			{
				toil.WithEffect(() => Effecter, base.TargetThingA);
			}
			if (Sound != null)
			{
				toil.PlaySustainerOrSound(Sound);
			}
			toil.AddPreTickAction(delegate
			{
				TickAction();
			});
			Toil toil2 = toil;
			toil2.initAction = (Action)Delegate.Combine(toil2.initAction, (Action)delegate
			{
				toil.actor.pather.StopDead();
			});
			Toil toil3 = toil;
			toil3.tickAction = (Action)Delegate.Combine(toil3.tickAction, (Action)delegate
			{
				pawn.rotationTracker.FaceTarget(base.TargetA);
			});
			Toil toil4 = toil;
			toil4.tickAction = (Action)Delegate.Combine(toil4.tickAction, (Action)delegate
			{
				progress = 1f - (float)ticksLeftThisToil / (float)totalTicks;
			});
			toil.AddFinishAction(delegate
			{
				
			});
			toil.handlingFacing = true;
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.socialMode = RandomSocialMode.Off;
			toil.defaultDuration = remainingTicks;
			return toil;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref progress, "progress", 0);
		}
	}

}
