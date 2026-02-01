using RimWorld;
using Verse;
using Verse.AI;

namespace NCLWorm;

public class JobGiver_CastAbilityToEnemyTarget : JobGiver_AICastAbility
{
	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		if (caster.mindState.enemyTarget != null && caster.mindState.enemyTarget.Spawned && ability.verb.CanHitTarget(caster.mindState.enemyTarget) && caster.CanSee(caster.mindState.enemyTarget))
		{
			return caster.mindState.enemyTarget;
		}
		return null;
	}
}
