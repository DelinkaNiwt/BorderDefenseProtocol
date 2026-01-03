using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_MechRepairMech : JobDriver
{
	private const TargetIndex MechInd = TargetIndex.A;

	private const int DefaultTicksPerHeal = 60;

	protected float ticksToNextRepair;

	protected Pawn Mech => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	protected virtual bool Remote => false;

	protected float TicksPerHeal => Mathf.RoundToInt(1f / pawn.GetStatValue(AncotDefOf.Ancot_MechRepairSpeed) * 60f);

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Mech, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (!ModLister.CheckBiotech("Mech repair"))
		{
			yield break;
		}
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnForbidden(TargetIndex.A);
		this.FailOn(() => Mech.IsAttacking());
		if (!Remote)
		{
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		}
		Toil toil = (Remote ? Toils_General.Wait(int.MaxValue) : Toils_General.WaitWith(TargetIndex.A, int.MaxValue, useProgressBar: false, maintainPosture: true, maintainSleep: true));
		toil.WithEffect(EffecterDefOf.MechRepairing, TargetIndex.A);
		toil.PlaySustainerOrSound(Remote ? SoundDefOf.RepairMech_Remote : SoundDefOf.RepairMech_Touch);
		toil.AddPreInitAction(delegate
		{
			ticksToNextRepair = TicksPerHeal;
		});
		toil.handlingFacing = true;
		toil.tickIntervalAction = delegate(int delta)
		{
			ticksToNextRepair -= delta;
			if (ticksToNextRepair <= 0f)
			{
				Mech.needs.energy.CurLevel -= Mech.GetStatValue(StatDefOf.MechEnergyLossPerHP);
				MechRepairUtility.RepairTick(Mech, 3);
				ticksToNextRepair += TicksPerHeal;
				if (ticksToNextRepair <= 0f)
				{
					ticksToNextRepair = TicksPerHeal;
				}
			}
			pawn.rotationTracker.FaceTarget(Mech);
		};
		toil.AddFinishAction(delegate
		{
			if (Mech.jobs?.curJob != null)
			{
				Mech.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		});
		toil.AddEndCondition(() => MechRepairUtility.CanRepair(Mech) ? JobCondition.Ongoing : JobCondition.Succeeded);
		yield return toil;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToNextRepair, "ticksToNextRepair", 0f);
	}
}
