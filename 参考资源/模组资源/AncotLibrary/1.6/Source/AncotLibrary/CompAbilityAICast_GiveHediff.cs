using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_GiveHediff : CompAbilityEffect_GiveHediff
{
	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return target.Pawn != null;
	}
}
