using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public abstract class JobDriver_GainHediffSelf : JobDriver
{
	public abstract HediffDef HediffDef { get; }

	public abstract int WarmupTicks { get; }

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(base.TargetThingA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.Wait(WarmupTicks);
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		yield return toil;
		yield return Toils_General.Do(GainHediff);
	}

	public virtual void GainHediff()
	{
		pawn.health.AddHediff(HediffDef);
	}
}
