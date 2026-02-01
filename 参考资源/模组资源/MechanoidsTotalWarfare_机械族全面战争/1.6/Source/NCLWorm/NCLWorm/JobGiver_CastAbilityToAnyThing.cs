using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCLWorm;

public class JobGiver_CastAbilityToAnyThing : JobGiver_AICastAbility
{
	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		IEnumerable<IAttackTarget> source = from t in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster)
			where !t.ThreatDisabled(caster) && ability.verb.CanHitTarget(t.Thing)
			select t;
		if (source.Any())
		{
			return source.RandomElement().Thing;
		}
		return null;
	}
}
