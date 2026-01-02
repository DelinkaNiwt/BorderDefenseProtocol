using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalNearbyAlly : ThinkNode_Conditional
{
	public float maxDistance = 14f;

	public int minCloseAlly = 1;

	protected override bool Satisfied(Pawn pawn)
	{
		if (pawn.Spawned && !pawn.Downed)
		{
			return NearbyAllies(pawn, 9, passDoors: true, maxDistance, minCloseAlly);
		}
		return false;
	}

	public static bool NearbyAllies(Pawn pawn, int regionsToScan = 9, bool passDoors = false, float maxDistance = -1f, int maxCount = 1)
	{
		TraverseParms tp = (passDoors ? TraverseParms.For(TraverseMode.PassDoors) : TraverseParms.For(pawn));
		int count = 0;
		RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate(Region r)
		{
			List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
			for (int i = 0; i < list.Count; i++)
			{
				if (pawn.Faction != null && list[i].Faction?.def == pawn.Faction.def && (maxDistance <= 0f || list[i].Position.InHorDistOf(pawn.Position, maxDistance)) && !(list[i] is Pawn { Downed: not false }))
				{
					count++;
				}
			}
			return count >= maxCount;
		}, regionsToScan);
		return count >= maxCount;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalNearbyAlly thinkNode_ConditionalNearbyAlly = (ThinkNode_ConditionalNearbyAlly)base.DeepCopy(resolve);
		thinkNode_ConditionalNearbyAlly.maxDistance = maxDistance;
		thinkNode_ConditionalNearbyAlly.minCloseAlly = minCloseAlly;
		return thinkNode_ConditionalNearbyAlly;
	}
}
