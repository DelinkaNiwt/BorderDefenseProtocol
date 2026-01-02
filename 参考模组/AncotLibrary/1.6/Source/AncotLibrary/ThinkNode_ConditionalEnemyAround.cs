using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalEnemyAround : ThinkNode_Conditional
{
	public float distance = 20f;

	public int minTargets = 2;

	protected override bool Satisfied(Pawn pawn)
	{
		List<Thing> list = new List<Thing>();
		if (pawn.Spawned && !pawn.Downed)
		{
			IEnumerable<Thing> enumerable = from x in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
				where x.Thing.Position.InHorDistOf(pawn.Position, distance)
				select x.Thing;
			if (enumerable.EnumerableNullOrEmpty())
			{
				return false;
			}
			return enumerable.Count() > minTargets;
		}
		return false;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalEnemyAround thinkNode_ConditionalEnemyAround = (ThinkNode_ConditionalEnemyAround)base.DeepCopy(resolve);
		thinkNode_ConditionalEnemyAround.distance = distance;
		thinkNode_ConditionalEnemyAround.minTargets = minTargets;
		return thinkNode_ConditionalEnemyAround;
	}
}
