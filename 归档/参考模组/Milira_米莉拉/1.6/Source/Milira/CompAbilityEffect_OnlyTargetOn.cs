using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Milira;

public class CompAbilityEffect_OnlyTargetOn : CompAbilityEffect
{
	private Pawn Pawn => parent.pawn;

	public new CompProperties_AbilityOnlyTargetOn Props => (CompProperties_AbilityOnlyTargetOn)props;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if ((Props.thingDefs.NullOrEmpty() && Props.thingDefs.Contains(target.Thing.def)) || (target.Thing is Pawn pawn && Props.pawnkindDefs.NullOrEmpty() && Props.pawnkindDefs.Contains(pawn.kindDef)))
		{
			return true;
		}
		return false;
	}

	public override bool CanApplyOn(GlobalTargetInfo target)
	{
		if ((Props.thingDefs.NullOrEmpty() && Props.thingDefs.Contains(target.Thing.def)) || (target.Thing is Pawn pawn && Props.pawnkindDefs.NullOrEmpty() && Props.pawnkindDefs.Contains(pawn.kindDef)))
		{
			return true;
		}
		return false;
	}
}
