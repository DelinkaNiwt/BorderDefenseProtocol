using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class JobGiver_Flick : ThinkNode_JobGiver
{
	public PathEndMode PathEndMode => PathEndMode.Touch;

	public IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		List<Designation> list = pawn.Map.designationManager.designationsByDef[DesignationDefOf.Flick];
		foreach (Designation item in list)
		{
			yield return item.target.Thing;
		}
	}

	public bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Flick);
	}

	public bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Flick) == null)
		{
			return false;
		}
		if (!pawn.CanReserve(t, 1, -1, null, forced))
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
		Predicate<Thing> validator = (Thing x) => HasJobOnThing(pawn, x);
		Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode, TraverseParms.For(pawn, Danger.Some), 100f, validator, PotentialWorkThingsGlobal(pawn));
		if (thing == null)
		{
			return null;
		}
		return JobMaker.MakeJob(JobDefOf.Flick, thing);
	}
}
