using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_AllyNearby : CompAbilityEffect
{
	private new CompProperties_AICast_AllyNearby Props => (CompProperties_AICast_AllyNearby)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Caster.Spawned && !Caster.Downed)
		{
			return AllyNearby(Caster, 9, passDoors: true, Props.maxDistance, Props.minCloseAlly);
		}
		return false;
	}

	public bool AllyNearby(Pawn pawn, int regionsToScan = 9, bool passDoors = false, float maxDistance = -1f, int maxCount = 1)
	{
		TraverseParms tp = (passDoors ? TraverseParms.For(TraverseMode.PassDoors) : TraverseParms.For(pawn));
		int count = 0;
		RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate(Region r)
		{
			List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Faction == pawn.Faction && (maxDistance <= 0f || list[i].Position.InHorDistOf(pawn.Position, maxDistance)))
				{
					Pawn pawn2 = list[i] as Pawn;
					if ((pawn2 == null || !pawn2.Downed) && (!Props.mechOnly || pawn2.RaceProps.IsMechanoid) && (!Props.fleshOnly || pawn2.RaceProps.IsFlesh))
					{
						count++;
					}
				}
			}
			return count >= maxCount;
		}, regionsToScan);
		return count >= maxCount;
	}
}
