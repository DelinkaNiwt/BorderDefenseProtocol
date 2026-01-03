using RimWorld;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

public class HediffComp_AutoStopMentalState : HediffComp
{
	public int tickcount;

	public HediffCompProperties_AutoStopMentalState Props => (HediffCompProperties_AutoStopMentalState)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		tickcount++;
		Pawn pawn = base.Pawn;
		if (pawn == null || tickcount % 120 != 0)
		{
			return;
		}
		Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.CatatonicBreakdown);
		if (firstHediffOfDef != null)
		{
			pawn.health.RemoveHediff(firstHediffOfDef);
		}
		if (pawn == null)
		{
			return;
		}
		MentalState mentalState = pawn.MentalState;
		if (mentalState != null)
		{
			mentalState.RecoverFromState();
			HediffDef named = DefDatabase<HediffDef>.GetNamed("CMC_HealingSE");
			if (named != null)
			{
				Hediff hediff = HediffMaker.MakeHediff(named, base.Pawn);
				hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 300;
				pawn.health.AddHediff(hediff);
			}
		}
	}
}
