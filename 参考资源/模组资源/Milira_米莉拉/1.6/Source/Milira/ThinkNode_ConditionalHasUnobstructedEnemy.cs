using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class ThinkNode_ConditionalHasUnobstructedEnemy : ThinkNode_Conditional
{
	private float detectionRadius = 20f;

	protected override bool Satisfied(Pawn pawn)
	{
		if (pawn == null || pawn.Map == null || pawn.Map.attackTargetsCache == null)
		{
			return false;
		}
		IEnumerable<Thing> enumerable = from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
			where !a.ThreatDisabled(pawn) && (float)(pawn.Position - a.Thing.Position).LengthHorizontalSquared <= detectionRadius * detectionRadius
			select a.Thing into t
			where t is Pawn || t is Building_Turret
			select t;
		if (enumerable == null)
		{
			return false;
		}
		foreach (Thing item in enumerable)
		{
			if (item != null && GenSight.LineOfSight(pawn.Position, item.Position, pawn.Map, skipFirstCell: true))
			{
				return true;
			}
		}
		return false;
	}
}
