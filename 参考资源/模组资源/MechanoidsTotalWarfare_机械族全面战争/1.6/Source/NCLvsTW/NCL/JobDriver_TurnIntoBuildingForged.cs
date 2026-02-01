using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

public class JobDriver_TurnIntoBuildingForged : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (pawn.Map == null || !pawn.Spawned)
		{
			Log.Error("Pawn " + pawn.Label + " is not in a valid state for transformation");
			yield break;
		}
		yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
		yield return new Toil
		{
			initAction = delegate
			{
				FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 2f);
			},
			defaultCompleteMode = ToilCompleteMode.Delay,
			defaultDuration = 60
		};
		yield return new Toil
		{
			initAction = delegate
			{
				if (pawn.abilities.GetAbility(NCLContainerDefOf.TurnIntoBuildingAbilityForged) is Ability_TurnIntoBuildingForged ability_TurnIntoBuildingForged)
				{
					ability_TurnIntoBuildingForged.Activate(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
				}
			},
			defaultCompleteMode = ToilCompleteMode.Instant
		};
	}
}
