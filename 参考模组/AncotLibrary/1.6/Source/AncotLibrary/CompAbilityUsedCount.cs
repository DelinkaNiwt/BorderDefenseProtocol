using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityUsedCount : CompAbilityEffect
{
	private int num = 0;

	public new CompProperties_AbilityUsedCount Props => (CompProperties_AbilityUsedCount)props;

	public Pawn Pawn => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public void UsedOnce()
	{
		num++;
		if (num < Props.totalNum)
		{
			return;
		}
		List<Hediff> list = Pawn.health.hediffSet.hediffs.Where((Hediff h) => h.def == Props.removeHediff).ToList();
		foreach (Hediff item in list)
		{
			Pawn.health.RemoveHediff(item);
		}
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref num, "num", 0);
	}
}
