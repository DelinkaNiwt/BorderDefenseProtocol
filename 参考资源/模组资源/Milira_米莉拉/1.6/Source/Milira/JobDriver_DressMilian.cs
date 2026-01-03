using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_DressMilian : JobDriver
{
	private const TargetIndex MilianInd = TargetIndex.A;

	private const TargetIndex ApparelInd = TargetIndex.B;

	private const int DurationTicks = 600;

	private Mote warmupMote;

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
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.WaitWith(TargetIndex.A, Apparel.GetStatValue(StatDefOf.EquipDelay).SecondsToTicks(), useProgressBar: true, maintainPosture: true);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		toil.tickAction = delegate
		{
			CompUsable compUsable = Apparel.TryGetComp<CompUsable>();
			if (compUsable != null && warmupMote == null && compUsable.Props.warmupMote != null)
			{
				warmupMote = MoteMaker.MakeAttachedOverlay(Milian, compUsable.Props.warmupMote, Vector3.zero);
			}
			warmupMote?.Maintain();
		};
		yield return toil;
		yield return Toils_General.Do(DressMilian);
	}

	private void DressMilian()
	{
		pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
		Milian.apparel.Wear(Apparel, dropReplacedApparel: true, locked: true);
	}
}
