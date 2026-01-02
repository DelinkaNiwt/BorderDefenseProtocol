using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityGravitational : CompAbilityEffect
{
	public new CompProperties_AbilityGravitational Props => (CompProperties_AbilityGravitational)props;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (target.Pawn != null)
		{
			ForceMovementUtility.ApplyGravitationalForce(parent.pawn.PositionHeld, target.Pawn, Props.distance, Props.removeHediffsAffected);
		}
	}
}
