using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalUnderCombatPressureIgnoreDowned : ThinkNode_Conditional
{
	public float maxThreatDistance = 2f;

	public int minCloseTargets = 2;

	public virtual float MaxThreatDistance => maxThreatDistance;

	protected override bool Satisfied(Pawn pawn)
	{
		if (pawn.Spawned && !pawn.Downed)
		{
			return EnemiesAreNearby(pawn, 9, passDoors: true, MaxThreatDistance, minCloseTargets);
		}
		return false;
	}

	public static bool EnemiesAreNearby(Pawn pawn, int regionsToScan = 9, bool passDoors = false, float maxDistance = -1f, int maxCount = 1)
	{
		TraverseParms tp = (passDoors ? TraverseParms.For(TraverseMode.PassDoors) : TraverseParms.For(pawn));
		int count = 0;
		RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate(Region r)
		{
			List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].HostileTo(pawn) && (maxDistance <= 0f || list[i].Position.InHorDistOf(pawn.Position, maxDistance)) && !(list[i] is Pawn { Downed: not false }))
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
		ThinkNode_ConditionalUnderCombatPressure thinkNode_ConditionalUnderCombatPressure = (ThinkNode_ConditionalUnderCombatPressure)base.DeepCopy(resolve);
		thinkNode_ConditionalUnderCombatPressure.maxThreatDistance = maxThreatDistance;
		thinkNode_ConditionalUnderCombatPressure.minCloseTargets = minCloseTargets;
		return thinkNode_ConditionalUnderCombatPressure;
	}
}
