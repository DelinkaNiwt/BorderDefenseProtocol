using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AICastAbilityThroughTarget : ThinkNode_JobGiver
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
			IntVec3 intVec = TargetPosition(pawn, enemyPosition, ability.verb.verbProps.range);
			return ability.GetJob(intVec, intVec);
		}
		return null;
	}

	public static IntVec3 TargetPosition(Pawn pawn, IntVec3 targetPposition, float maxDistance)
	{
		IntVec3 position = pawn.Position;
		IntVec3 intVec = targetPposition - position;
		IntVec3 result = position;
		Vector3 vector = intVec.ToVector3();
		vector.Normalize();
		Map map = pawn.Map;
		for (int i = 0; (float)i < maxDistance; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec2 = position + vect.ToIntVec3();
			if (!ValidJumpTarget(map, intVec2))
			{
				break;
			}
			result = intVec2;
		}
		return result;
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

	public static bool ValidJumpTarget(Map map, IntVec3 cell)
	{
		if (!cell.IsValid || !cell.InBounds(map))
		{
			return false;
		}
		if (cell.Impassable(map) || !cell.Walkable(map) || cell.Fogged(map))
		{
			return false;
		}
		Building edifice = cell.GetEdifice(map);
		if (edifice != null && edifice is Building_Door { Open: false })
		{
			return false;
		}
		return true;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_AICastAbilityThroughTarget jobGiver_AICastAbilityThroughTarget = (JobGiver_AICastAbilityThroughTarget)base.DeepCopy(resolve);
		jobGiver_AICastAbilityThroughTarget.ability = ability;
		jobGiver_AICastAbilityThroughTarget.detectionRadius = detectionRadius;
		jobGiver_AICastAbilityThroughTarget.minDistanceFromEnemy = minDistanceFromEnemy;
		return jobGiver_AICastAbilityThroughTarget;
	}
}
