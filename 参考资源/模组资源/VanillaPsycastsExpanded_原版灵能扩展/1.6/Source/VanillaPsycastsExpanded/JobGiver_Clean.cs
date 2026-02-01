using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class JobGiver_Clean : ThinkNode_JobGiver
{
	private int MinTicksSinceThickened = 600;

	public PathEndMode PathEndMode => PathEndMode.Touch;

	public IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.listerFilthInHomeArea.FilthInHomeArea;
	}

	public bool ShouldSkip(Pawn pawn)
	{
		return pawn.Map.listerFilthInHomeArea.FilthInHomeArea.Count == 0;
	}

	public bool HasJobOnThing(Pawn pawn, Thing t)
	{
		if (!(t is Filth filth))
		{
			return false;
		}
		if (!filth.Map.areaManager.Home[filth.Position])
		{
			return false;
		}
		if (!pawn.CanReserve(t))
		{
			return false;
		}
		if (filth.TicksSinceThickened < MinTicksSinceThickened)
		{
			return false;
		}
		return true;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (ShouldSkip(pawn))
		{
			return null;
		}
		Predicate<Thing> validator = (Thing x) => x.def.category == ThingCategory.Filth && HasJobOnThing(pawn, x);
		Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Filth), PathEndMode, TraverseParms.For(pawn, Danger.Some), 100f, validator, PotentialWorkThingsGlobal(pawn));
		if (thing == null)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Clean);
		job.AddQueuedTarget(TargetIndex.A, thing);
		int num = 15;
		Map map = thing.Map;
		Room room = thing.GetRoom();
		for (int num2 = 0; num2 < 100; num2++)
		{
			IntVec3 c = thing.Position + GenRadial.RadialPattern[num2];
			if (!ShouldClean(c))
			{
				continue;
			}
			List<Thing> thingList = c.GetThingList(map);
			for (int num3 = 0; num3 < thingList.Count; num3++)
			{
				Thing thing2 = thingList[num3];
				if (HasJobOnThing(pawn, thing2) && thing2 != thing)
				{
					job.AddQueuedTarget(TargetIndex.A, thing2);
				}
			}
			if (job.GetTargetQueue(TargetIndex.A).Count >= num)
			{
				break;
			}
		}
		if (job.targetQueueA != null && job.targetQueueA.Count >= 5)
		{
			job.targetQueueA.SortBy((LocalTargetInfo targ) => targ.Cell.DistanceToSquared(pawn.Position));
		}
		return job;
		bool ShouldClean(IntVec3 intVec)
		{
			if (!intVec.InBounds(map))
			{
				return false;
			}
			Room room2 = intVec.GetRoom(map);
			if (room == room2)
			{
				return true;
			}
			Region region = intVec.GetDoor(map)?.GetRegion(RegionType.Portal);
			if (region != null && !region.links.NullOrEmpty())
			{
				for (int i = 0; i < region.links.Count; i++)
				{
					RegionLink regionLink = region.links[i];
					for (int j = 0; j < 2; j++)
					{
						if (regionLink.regions[j] != null && regionLink.regions[j] != region && regionLink.regions[j].valid && regionLink.regions[j].Room == room)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
