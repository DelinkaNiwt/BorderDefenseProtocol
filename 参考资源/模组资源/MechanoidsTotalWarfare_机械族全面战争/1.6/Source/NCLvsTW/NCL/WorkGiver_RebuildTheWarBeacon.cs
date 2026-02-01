using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

internal class WorkGiver_RebuildTheWarBeacon : WorkGiver_Scanner
{
	private static string NoWortTrans;

	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("Building_Ancient_WarBeacon"));

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public static void ResetStaticData()
	{
		NoWortTrans = "NCL_WARBEACON_NO_MATERIAL".Translate();
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Building_Ancient_WarBeacon Building_Ancient_WarBeacon) || Building_Ancient_WarBeacon.requiredThings.NullOrEmpty() || !Building_Ancient_WarBeacon.allowFilling)
		{
			return false;
		}
		if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
		{
			return false;
		}
		if (FindThingToFill(pawn, Building_Ancient_WarBeacon) == null)
		{
			JobFailReason.Is(NoWortTrans);
			return false;
		}
		bool flag5 = t.IsBurning();
		return !flag5;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Building_Ancient_WarBeacon buinding = (Building_Ancient_WarBeacon)t;
		Thing t2 = FindThingToFill(pawn, buinding);
		return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("RebuildTheWarBeacon"), t, t2);
	}

	private Thing FindThingToFill(Pawn pawn, Building_Ancient_WarBeacon buinding)
	{
		Predicate<Thing> validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x);
		Thing thing = null;
		foreach (ThingDefCountClass thingDefCountClass in buinding.requiredThings)
		{
			thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(thingDefCountClass.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
			if (thing != null)
			{
				return thing;
			}
		}
		return thing;
	}
}
