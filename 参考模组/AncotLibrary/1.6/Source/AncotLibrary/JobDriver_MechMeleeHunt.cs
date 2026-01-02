using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_MechMeleeHunt : JobDriver
{
	private int jobStartTick = -1;

	private bool firstHit = true;

	private const TargetIndex VictimInd = TargetIndex.A;

	private const TargetIndex CorpseInd = TargetIndex.A;

	private const TargetIndex StoreCellInd = TargetIndex.B;

	public const float MaxRangeFactor = 0.95f;

	public Pawn Victim
	{
		get
		{
			Corpse corpse = Corpse;
			if (corpse != null)
			{
				return corpse.InnerPawn;
			}
			return (Pawn)job.GetTarget(TargetIndex.A).Thing;
		}
	}

	private Corpse Corpse => job.GetTarget(TargetIndex.A).Thing as Corpse;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref firstHit, "firstHit", defaultValue: false);
		Scribe_Values.Look(ref jobStartTick, "jobStartTick", 0);
	}

	public override string GetReport()
	{
		if (Victim != null)
		{
			return JobUtility.GetResolvedJobReport(job.def.reportString, Victim);
		}
		return base.GetReport();
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Victim, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOn(delegate
		{
			if (Victim == null)
			{
				return true;
			}
			if (Victim.IsForbidden(pawn))
			{
				return true;
			}
			if (!job.ignoreDesignations)
			{
				Pawn victim = Victim;
				if (victim != null && !victim.Dead && base.Map.designationManager.DesignationOn(victim, DesignationDefOf.Hunt) == null)
				{
					return true;
				}
			}
			return false;
		});
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			jobStartTick = Find.TickManager.TicksGame;
		};
		yield return toil;
		Action hitAction = delegate
		{
			Pawn victim = Victim;
			bool surpriseAttack = firstHit && victim.AnimalOrWildMan();
			if (pawn.meleeVerbs.TryMeleeAttack(victim, job.verbToUse, surpriseAttack))
			{
				base.Map.attackTargetsCache.UpdateTarget(pawn);
				firstHit = false;
			}
		};
		Toil startCollectCorpseLabel = Toils_General.Label();
		Toil slaughterLabel = Toils_General.Label();
		Toil toil_FollowAndMeleeAttack = Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, hitAction).JumpIfDespawnedOrNull(TargetIndex.A, startCollectCorpseLabel).JumpIf(() => Corpse != null, startCollectCorpseLabel)
			.FailOn(() => Find.TickManager.TicksGame > jobStartTick + 5000 && (float)(job.GetTarget(TargetIndex.A).Cell - pawn.Position).LengthHorizontalSquared > 4f);
		Toil slaughterIfPossible = Toils_Jump.JumpIf(slaughterLabel, delegate
		{
			Pawn victim = Victim;
			return (victim.RaceProps.DeathActionWorker == null || !victim.RaceProps.DeathActionWorker.DangerousInMelee) && victim.Downed;
		});
		yield return slaughterIfPossible;
		yield return toil_FollowAndMeleeAttack.JumpIfDespawnedOrNull(TargetIndex.A, startCollectCorpseLabel);
		yield return Toils_Jump.JumpIfTargetDespawnedOrNull(TargetIndex.A, startCollectCorpseLabel);
		yield return Toils_Jump.Jump(slaughterIfPossible);
		yield return slaughterLabel;
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnMobile(TargetIndex.A);
		yield return Toils_General.WaitWith(TargetIndex.A, 180, useProgressBar: true).FailOnMobile(TargetIndex.A);
		yield return Toils_General.Do(delegate
		{
			if (!Victim.Dead)
			{
				ExecutionUtility.DoHuntingExecution(pawn, Victim);
				pawn.records.Increment(RecordDefOf.AnimalsSlaughtered);
				if (pawn.InMentalState)
				{
					pawn.MentalState.Notify_SlaughteredTarget();
				}
			}
		});
		yield return Toils_Jump.Jump(startCollectCorpseLabel);
		yield return startCollectCorpseLabel;
		yield return StartCollectCorpseToil();
		yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.A);
		Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
		yield return carryToCell;
		yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, storageMode: true);
	}

	private Toil StartCollectCorpseToil()
	{
		Toil toil = ToilMaker.MakeToil("StartCollectCorpseToil");
		toil.initAction = delegate
		{
			if (Victim == null)
			{
				toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
			}
			else
			{
				TaleRecorder.RecordTale(TaleDefOf.Hunted, pawn, Victim);
				Corpse corpse = Victim.Corpse;
				if (corpse == null || !pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
				else
				{
					corpse.SetForbidden(value: false);
					if (StoreUtility.TryFindBestBetterStoreCellFor(corpse, pawn, base.Map, StoragePriority.Unstored, pawn.Faction, out var foundCell))
					{
						pawn.Reserve(corpse, job);
						pawn.Reserve(foundCell, job);
						job.SetTarget(TargetIndex.B, foundCell);
						job.SetTarget(TargetIndex.A, corpse);
						job.count = 1;
						job.haulMode = HaulMode.ToCellStorage;
					}
					else
					{
						pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
					}
				}
			}
		};
		return toil;
	}
}
