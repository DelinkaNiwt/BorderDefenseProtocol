using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Verb_Invisible : Verb_CastAbility
{
	protected override bool TryCastShot()
	{
		HediffDef named = DefDatabase<HediffDef>.GetNamed("PsychicInvisibility");
		if (named != null && CasterPawn != null)
		{
			Hediff hediff = HediffMaker.MakeHediff(named, CasterPawn);
			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 3600;
			CasterPawn.health.AddHediff(hediff);
			base.ReloadableCompSource?.UsedOnce();
			return true;
		}
		return false;
	}
}
