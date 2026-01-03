using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_StripMilian_Apparel : JobDriver
{
	private const TargetIndex MilianInd = TargetIndex.A;

	private const TargetIndex ApparelInd = TargetIndex.B;

	private const int DurationTicks = 600;

	private Pawn Milian => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (pawn.Reserve(Milian, job, 1, -1, null, errorOnFailed))
		{
			return pawn.Reserve(Apparel, job, 1, -1, null, errorOnFailed);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.WaitWith(TargetIndex.A, Apparel.GetStatValue(StatDefOf.EquipDelay).SecondsToTicks(), useProgressBar: true, maintainPosture: true);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		yield return toil;
		yield return Toils_General.Do(StripMilian);
	}

	private void StripMilian()
	{
		Milian.apparel.TryDrop(Apparel);
	}
}
