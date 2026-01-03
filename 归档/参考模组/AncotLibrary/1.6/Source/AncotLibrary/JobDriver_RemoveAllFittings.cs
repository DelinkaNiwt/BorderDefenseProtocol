using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_RemoveAllFittings : JobDriver
{
	public ThingWithComps Weapon
	{
		get
		{
			if (job.GetTarget(TargetIndex.A).Thing is Pawn)
			{
				return job.GetTarget(TargetIndex.B).Thing as ThingWithComps;
			}
			return job.GetTarget(TargetIndex.A).Thing as ThingWithComps;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(base.TargetThingA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (job.GetTarget(TargetIndex.A).Thing != pawn)
		{
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		}
		Toil toil = Toils_General.WaitWith(TargetIndex.A, 30, useProgressBar: true, maintainPosture: true, maintainSleep: false, TargetIndex.A);
		if (pawn == base.TargetThingA)
		{
			toil = Toils_General.Wait(30);
		}
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		if (job.GetTarget(TargetIndex.A).Thing != pawn)
		{
			toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		}
		yield return toil;
		yield return Toils_General.Do(Remove);
	}

	public virtual void Remove()
	{
		CompUniqueWeapon compUniqueWeapon = Weapon?.TryGetComp<CompUniqueWeapon>();
		if (Weapon != null && compUniqueWeapon != null)
		{
			WeaponTraitsUtility.RemoveAllTraitsAndDropFittings(Weapon, pawn);
		}
	}
}
