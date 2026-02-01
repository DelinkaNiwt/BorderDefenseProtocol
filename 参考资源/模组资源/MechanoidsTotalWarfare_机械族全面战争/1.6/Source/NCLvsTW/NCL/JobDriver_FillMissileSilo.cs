using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class JobDriver_FillMissileSilo : JobDriver
{
	protected Thing Silo => job.GetTarget(TargetIndex.A).Thing;

	protected Thing Resource => job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Silo, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Resource, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOnBurningImmobile(TargetIndex.A);
		this.FailOn(() => Silo.TryGetComp<CompSteelResource>().AmountToAutofill <= 0);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
		yield return Toils_General.Wait(25).WithProgressBarToilDelay(TargetIndex.A);
		yield return new Toil
		{
			initAction = delegate
			{
				Thing thing = pawn.CurJob.GetTarget(TargetIndex.B).Thing;
				int num = Mathf.Min(thing.stackCount, Silo.TryGetComp<CompSteelResource>().AmountToAutofill);
				if (num > 0)
				{
					Silo.TryGetComp<CompSteelResource>().AddIngredient(thing.def, num);
					thing.SplitOff(num).Destroy();
				}
			}
		};
	}
}
