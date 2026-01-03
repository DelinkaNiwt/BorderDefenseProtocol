using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_UnderCombatPressure : CompAbilityEffect
{
	private new CompProperties_AICast_UnderCombatPressure Props => (CompProperties_AICast_UnderCombatPressure)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Caster.Spawned && !Caster.Downed)
		{
			return EnemiesAreNearby(Caster, 9, passDoors: true, Props.maxThreatDistance, Props.minCloseTargets);
		}
		return false;
	}

	public bool EnemiesAreNearby(Pawn pawn, int regionsToScan = 9, bool passDoors = false, float maxDistance = -1f, int maxCount = 1)
	{
		TraverseParms tp = (passDoors ? TraverseParms.For(TraverseMode.PassDoors) : TraverseParms.For(pawn));
		int count = 0;
		RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate(Region r)
		{
			List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].HostileTo(pawn) && (maxDistance <= 0f || list[i].Position.InHorDistOf(pawn.Position, maxDistance)) && list[i] is Pawn { Downed: false } pawn2 && (!Props.mechOnly || pawn2.RaceProps.IsMechanoid) && (!Props.fleshOnly || pawn2.RaceProps.IsFlesh))
				{
					count++;
				}
			}
			return count >= maxCount;
		}, regionsToScan);
		return count >= maxCount;
	}
}
