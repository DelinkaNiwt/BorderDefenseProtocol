using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class WorkGiver_RepairDrone : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
	}

	public override Danger MaxPathDanger(Pawn pawn)
	{
		return Danger.Deadly;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return false;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Pawn pawn2 = (Pawn)t;
		CompDrone compDrone = pawn2.TryGetComp<CompDrone>();
		if (compDrone == null)
		{
			return false;
		}
		if (!pawn2.RaceProps.IsMechanoid)
		{
			return false;
		}
		if (pawn2.InAggroMentalState || pawn2.HostileTo(pawn))
		{
			return false;
		}
		if (!pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		if (pawn2.IsBurning())
		{
			return false;
		}
		if (pawn2.IsAttacking())
		{
			return false;
		}
		if (!compDrone.CanRepair())
		{
			return false;
		}
		if (!forced)
		{
			return compDrone.autoRepair;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		return JobMaker.MakeJob(AncotJobDefOf.Ancot_RepairDrone, t);
	}
}
