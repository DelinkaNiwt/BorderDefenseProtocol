using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test;

public class HediffComp_AutoHeal : HediffComp
{
	public int tickcount;

	public bool flag = false;

	public HediffCompProperties_AutoHeal Props => (HediffCompProperties_AutoHeal)props;

	private bool CanHeal => base.Pawn.health.hediffSet.hediffs.Find((Hediff x) => x is Hediff_Injury hd && (hd.CanHealNaturally() || hd.CanHealFromTending())) != null;

	public override void CompPostTick(ref float severityAdjustment)
	{
		tickcount++;
		Pawn pawn = base.Pawn;
		if (tickcount % 360 == 0 && pawn != null && !pawn.Dead && CanHeal)
		{
			TryHealInjury(pawn);
		}
	}

	public void TryHealInjury(Pawn pawn)
	{
		flag = false;
		List<Hediff_Injury> resultHediffs = new List<Hediff_Injury>();
		pawn.health.hediffSet.GetHediffs(ref resultHediffs, (Hediff_Injury x) => x.CanHealNaturally() || x.CanHealFromTending());
		if (resultHediffs.TryRandomElement(out var result))
		{
			result.Heal(20f);
			Log.Message("Healed");
			flag = true;
		}
		if (flag)
		{
			HediffDef named = DefDatabase<HediffDef>.GetNamed("CMC_HealingSE");
			Hediff hediff = HediffMaker.MakeHediff(named, base.Pawn);
			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 300;
			pawn.health.AddHediff(hediff);
		}
	}
}
