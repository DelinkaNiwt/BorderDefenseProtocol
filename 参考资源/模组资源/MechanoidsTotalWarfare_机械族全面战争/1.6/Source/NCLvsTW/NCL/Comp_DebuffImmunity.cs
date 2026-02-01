using System.Collections.Generic;
using Verse;

namespace NCL;

public class Comp_DebuffImmunity : HediffComp
{
	public CompProperties_DebuffImmunity Props => (CompProperties_DebuffImmunity)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (Find.TickManager.TicksGame % 1 == 0 && base.Pawn != null && !base.Pawn.Dead)
		{
			RemoveAllBadHediffs(base.Pawn);
		}
	}

	private void RemoveAllBadHediffs(Pawn pawn)
	{
		List<Hediff> toRemove = new List<Hediff>();
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			if (IsBadHediff(hediff))
			{
				toRemove.Add(hediff);
			}
		}
		foreach (Hediff hediff2 in toRemove)
		{
			pawn.health.RemoveHediff(hediff2);
		}
	}

	private bool IsBadHediff(Hediff hediff)
	{
		return hediff.def.isBad && !(hediff is Hediff_Injury) && !(hediff is Hediff_MissingPart);
	}
}
