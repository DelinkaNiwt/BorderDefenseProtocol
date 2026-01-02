using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AIJumpToRandomEnemy : ThinkNode_JobGiver
{
	private AbilityDef ability;

	private float detectionRadius = 20f;

	private float minDistanceFromEnemy = 3f;

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.Downed)
		{
			return null;
		}
		Ability ability = pawn.abilities?.GetAbility(this.ability, includeTemporary: true);
		if (ability == null || !ability.CanCast)
		{
			return null;
		}
		if (TryFindEnemy(pawn, out var thing, ability.verb.verbProps.range))
		{
			return ability.GetJob(thing, thing);
		}
		return null;
	}

	private bool TryFindEnemy(Pawn pawn, out Thing thing, float maxDistance)
	{
		IEnumerable<Thing> source = from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
			where !a.ThreatDisabled(pawn) && (float)(pawn.Position - a.Thing.Position).LengthHorizontalSquared <= detectionRadius * detectionRadius
			select a.Thing into t
			where t is Pawn || t is Building_Turret || t is Building_Aerocraft
			select t;
		thing = source.RandomElement();
		if (thing != null)
		{
			float num = (pawn.Position - thing.Position).LengthHorizontalSquared;
			if (num > minDistanceFromEnemy * minDistanceFromEnemy)
			{
				return true;
			}
		}
		return false;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_AIJumpToRandomEnemy jobGiver_AIJumpToRandomEnemy = (JobGiver_AIJumpToRandomEnemy)base.DeepCopy(resolve);
		jobGiver_AIJumpToRandomEnemy.ability = ability;
		jobGiver_AIJumpToRandomEnemy.detectionRadius = detectionRadius;
		jobGiver_AIJumpToRandomEnemy.minDistanceFromEnemy = minDistanceFromEnemy;
		return jobGiver_AIJumpToRandomEnemy;
	}
}
