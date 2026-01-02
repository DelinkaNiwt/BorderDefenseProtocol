using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_ConsumeTravelRation : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(base.TargetThingA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.Wait(30);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		yield return toil;
		yield return Toils_General.Do(ConsumedFood);
	}

	private void ConsumedFood()
	{
		Thing thing = ThingMaker.MakeThing(MiliraDefOf.Milira_TravellerFood);
		if (pawn.needs?.food != null)
		{
			pawn.needs.food.CurLevel += thing.GetStatValue(StatDefOf.Nutrition);
		}
		thing.Ingested(pawn, thing.GetStatValue(StatDefOf.Nutrition));
	}
}
