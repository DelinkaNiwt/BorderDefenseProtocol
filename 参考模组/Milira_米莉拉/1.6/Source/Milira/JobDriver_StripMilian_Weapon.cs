using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_StripMilian_Weapon : JobDriver
{
	private const TargetIndex MilianInd = TargetIndex.A;

	private const TargetIndex ApparelInd = TargetIndex.B;

	private const int DurationTicks = 600;

	private Pawn Milian => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	private ThingWithComps Weapon => (ThingWithComps)job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Milian, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.WaitWith(TargetIndex.A, 60, useProgressBar: true, maintainPosture: true);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		yield return toil;
		yield return Toils_General.Do(StripMilian);
	}

	private void StripMilian()
	{
		ThingWithComps thingWithComps = Milian.equipment?.Primary;
		if (thingWithComps != null)
		{
			Milian.equipment.TryDropEquipment(thingWithComps, out var _, pawn.Position);
		}
	}
}
