using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_RepairDrone : JobDriver
{
	private const TargetIndex MechInd = TargetIndex.A;

	private const int DefaultTicksPerHeal = 120;

	protected int ticksToNextRepair;

	protected Pawn Mech => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	protected virtual bool Remote => false;

	protected int TicksPerHeal => Mathf.RoundToInt(1f / pawn.GetStatValue(StatDefOf.MechRepairSpeed) * 120f);

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Mech, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
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
			if (ticksToNextRepair <= 0)
			{
				MechRepairUtility.RepairTick(Mech, delta);
				ticksToNextRepair = TicksPerHeal;
			}
			pawn.rotationTracker.FaceTarget(Mech);
			pawn.skills?.Learn(SkillDefOf.Crafting, 0.05f * (float)delta);
		};
		toil.AddFinishAction(delegate
		{
			if (Mech.jobs?.curJob != null)
			{
				Mech.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		});
		toil.AddEndCondition(delegate
		{
			CompDrone compDrone = Mech.TryGetComp<CompDrone>();
			return (compDrone != null && compDrone.CanRepair()) ? JobCondition.Ongoing : JobCondition.Succeeded;
		});
		if (!Remote)
		{
			toil.activeSkill = () => SkillDefOf.Crafting;
		}
		yield return toil;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToNextRepair, "ticksToNextRepair", 0);
	}
}
