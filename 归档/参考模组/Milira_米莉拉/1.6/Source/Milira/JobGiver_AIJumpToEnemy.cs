using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AIJumpToEnemy : ThinkNode_JobGiver
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
		if (TryFindEnemyPosition(pawn, out var enemyPosition, ability.verb.verbProps.range))
		{
			return ability.GetJob(enemyPosition, enemyPosition);
		}
		return null;
	}

	private bool TryFindEnemyPosition(Pawn pawn, out IntVec3 enemyPosition, float maxDistance)
	{
		IEnumerable<Thing> source = from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
			where !a.ThreatDisabled(pawn) && (float)(pawn.Position - a.Thing.Position).LengthHorizontalSquared <= detectionRadius * detectionRadius
			select a.Thing into t
			where t is Pawn || t is Building_Turret || t is Building_Aerocraft
			select t;
		Thing thing = source.OrderBy((Thing e) => (pawn.Position - e.Position).LengthHorizontalSquared).FirstOrDefault();
		if (thing != null)
		{
			float num = (pawn.Position - thing.Position).LengthHorizontalSquared;
			if (num > minDistanceFromEnemy * minDistanceFromEnemy)
			{
				enemyPosition = thing.Position;
				return true;
			}
		}
		enemyPosition = IntVec3.Invalid;
		return false;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_AIJumpToEnemy jobGiver_AIJumpToEnemy = (JobGiver_AIJumpToEnemy)base.DeepCopy(resolve);
		jobGiver_AIJumpToEnemy.ability = ability;
		jobGiver_AIJumpToEnemy.detectionRadius = detectionRadius;
		jobGiver_AIJumpToEnemy.minDistanceFromEnemy = minDistanceFromEnemy;
		return jobGiver_AIJumpToEnemy;
	}
}
