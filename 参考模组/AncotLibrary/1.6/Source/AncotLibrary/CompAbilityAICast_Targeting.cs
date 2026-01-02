using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_Targeting : CompAbilityEffect
{
	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return target.Thing != null;
	}
}
