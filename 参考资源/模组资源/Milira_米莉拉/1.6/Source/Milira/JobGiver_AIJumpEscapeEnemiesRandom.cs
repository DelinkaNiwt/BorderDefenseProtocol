using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AIJumpEscapeEnemiesRandom : ThinkNode_JobGiver
{
	private AbilityDef ability;

	public float minEscapeRangeFactor = 0.5f;

	public float maxEscapeRangeFactor = 1f;

	private static List<Thing> tmpHostileSpots = new List<Thing>();

	protected override Job TryGiveJob(Pawn pawn)
	{
		Ability ability = pawn.abilities?.GetAbility(this.ability, includeTemporary: true);
		if (ability == null || !ability.CanCast)
		{
			return null;
		}
		float num = Rand.Range(minEscapeRangeFactor, maxEscapeRangeFactor);
		if (TryFindRelocatePosition(pawn, out var relocatePosition, ability.verb.verbProps.range * num))
		{
			return ability.GetJob(relocatePosition, relocatePosition);
		}
		return null;
	}

	private bool TryFindRelocatePosition(Pawn pawn, out IntVec3 relocatePosition, float maxDistance)
	{
		tmpHostileSpots.Clear();
		tmpHostileSpots.AddRange(from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
			where !a.ThreatDisabled(pawn)
			select a.Thing);
		Ability jump = pawn.abilities?.GetAbility(ability, includeTemporary: true);
		relocatePosition = CellFinderLoose.GetFallbackDest(pawn, tmpHostileSpots, maxDistance, 5f, 5f, 20, (IntVec3 c) => jump.verb.ValidateTarget(c, showMessages: false));
		tmpHostileSpots.Clear();
		return relocatePosition.IsValid;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_AIJumpEscapeEnemiesRandom jobGiver_AIJumpEscapeEnemiesRandom = (JobGiver_AIJumpEscapeEnemiesRandom)base.DeepCopy(resolve);
		jobGiver_AIJumpEscapeEnemiesRandom.ability = ability;
		jobGiver_AIJumpEscapeEnemiesRandom.minEscapeRangeFactor = minEscapeRangeFactor;
		jobGiver_AIJumpEscapeEnemiesRandom.maxEscapeRangeFactor = maxEscapeRangeFactor;
		return jobGiver_AIJumpEscapeEnemiesRandom;
	}
}
