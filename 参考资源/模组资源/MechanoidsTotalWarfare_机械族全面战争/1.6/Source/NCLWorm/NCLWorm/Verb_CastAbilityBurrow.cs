using RimWorld;
using Verse;

namespace NCLWorm;

public class Verb_CastAbilityBurrow : Verb_CastAbilityJump
{
	protected override bool TryCastShot()
	{
		if (ability.Activate(currentTarget, currentDestination))
		{
			return JumpUtility.DoJump(CasterPawn, currentTarget, base.ReloadableCompSource, verbProps, ability, base.CurrentTarget, DefDatabase<ThingDef>.GetNamed("NCLBurrow_Flyer"));
		}
		return false;
	}
}
