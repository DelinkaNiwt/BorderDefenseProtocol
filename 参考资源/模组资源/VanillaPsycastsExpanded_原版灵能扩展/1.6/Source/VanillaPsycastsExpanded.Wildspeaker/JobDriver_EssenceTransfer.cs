using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class JobDriver_EssenceTransfer : JobDriver
{
	private int restStartTick;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		this.FailOn(() => !(base.TargetA.Thing is Corpse));
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		Toil toil = Toils_LayDown.LayDown(TargetIndex.B, hasBed: false, lookForOtherJobs: false, canSleep: true, gainRestAndHealth: false);
		toil.AddPreInitAction(delegate
		{
			restStartTick = Find.TickManager.TicksGame;
		});
		toil.AddPreTickAction(delegate
		{
			if (Find.TickManager.TicksGame >= restStartTick + 15000)
			{
				ReadyForNextToil();
			}
		});
		yield return toil;
		yield return Toils_General.Do(delegate
		{
			ResurrectionUtility.TryResurrect((base.TargetA.Thing as Corpse).InnerPawn);
			pawn.Kill(null, null);
		});
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref restStartTick, "restStartTick", 0);
	}
}
