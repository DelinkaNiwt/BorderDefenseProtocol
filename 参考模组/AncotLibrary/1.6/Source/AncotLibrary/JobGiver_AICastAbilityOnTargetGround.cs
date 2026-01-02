using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AICastAbilityOnTargetGround : ThinkNode_JobGiver
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
		if (TryFindEnemyPosition(pawn, out var enemyPosition))
		{
			IntVec3 intVec = enemyPosition;
			return ability.GetJob(intVec, intVec);
		}
		return null;
	}

	private bool TryFindEnemyPosition(Pawn pawn, out IntVec3 enemyPosition)
	{
		IEnumerable<Thing> source = from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
			where !a.ThreatDisabled(pawn) && (float)(pawn.Position - a.Thing.Position).LengthHorizontalSquared <= detectionRadius * detectionRadius
			select a.Thing into t
			where t is Pawn || t is Building_Turret
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
		JobGiver_AICastAbilityOnTargetGround jobGiver_AICastAbilityOnTargetGround = (JobGiver_AICastAbilityOnTargetGround)base.DeepCopy(resolve);
		jobGiver_AICastAbilityOnTargetGround.ability = ability;
		jobGiver_AICastAbilityOnTargetGround.detectionRadius = detectionRadius;
		jobGiver_AICastAbilityOnTargetGround.minDistanceFromEnemy = minDistanceFromEnemy;
		return jobGiver_AICastAbilityOnTargetGround;
	}
}
