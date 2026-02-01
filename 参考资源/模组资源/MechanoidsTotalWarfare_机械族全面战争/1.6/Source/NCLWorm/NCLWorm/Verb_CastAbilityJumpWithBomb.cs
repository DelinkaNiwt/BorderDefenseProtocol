using RimWorld;
using Verse;

namespace NCLWorm;

public class Verb_CastAbilityJumpWithBomb : Verb_CastAbilityJump
{
	protected override bool TryCastShot()
	{
		if (ability.Activate(currentTarget, currentDestination))
		{
			return JumpUtility.DoJump(CasterPawn, currentTarget, base.ReloadableCompSource, verbProps, ability, base.CurrentTarget, DefDatabase<ThingDef>.GetNamed("NCLJumpWithBomb_Flyer"));
		}
		return false;
	}
}
