using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_SynaesthesiaHarmonicFrequency : JobDriver
{
	protected int RepairTicks
	{
		get
		{
			if (pawn.Drafted)
			{
				return 999999;
			}
			return 600;
		}
	}

	protected Pawn mech => job.GetTarget(TargetIndex.A).Pawn;

	private float Range => 24.9f;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(mech, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnForbidden(TargetIndex.A);
		this.FailOn(() => mech.Position.DistanceTo(pawn.Position) > Range);
		this.FailOn(() => mech?.Faction != pawn.Faction);
		Toil toil = Toils_General.Wait(RepairTicks);
		toil.handlingFacing = true;
		toil.tickAction = delegate
		{
			Hediff firstHediffOfDef = mech.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milira_SynaesthesiaHarmonicFrequency);
			if (firstHediffOfDef == null)
			{
				mech.health.AddHediff(MiliraDefOf.Milira_SynaesthesiaHarmonicFrequency);
			}
			else
			{
				firstHediffOfDef.Severity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) / 2f;
			}
			pawn.rotationTracker.FaceTarget(mech);
		};
		toil.AddFinishAction(delegate
		{
			Hediff hediff = mech?.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milira_SynaesthesiaHarmonicFrequency);
			if (hediff != null)
			{
				hediff.Severity -= hediff.Severity;
			}
			if (mech.Dead)
			{
				Log.Message("1111");
				pawn.needs?.mood?.thoughts?.memories.TryGainMemory(MiliraDefOf.Milira_DeathFeeling);
			}
		});
		yield return toil;
	}
}
