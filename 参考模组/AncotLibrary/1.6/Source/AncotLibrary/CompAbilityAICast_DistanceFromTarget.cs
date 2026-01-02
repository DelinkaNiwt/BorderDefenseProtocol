using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_DistanceFromTarget : CompAbilityEffect
{
	private new CompProperties_AICast_DistanceFromTarget Props => (CompProperties_AICast_DistanceFromTarget)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (target == null)
		{
			return false;
		}
		if (Props.distance.Includes(Caster.Position.DistanceTo(target.Thing.Position)))
		{
			return true;
		}
		return false;
	}
}
