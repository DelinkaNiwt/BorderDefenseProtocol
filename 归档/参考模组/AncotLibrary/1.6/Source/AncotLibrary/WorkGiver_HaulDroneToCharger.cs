using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class WorkGiver_HaulDroneToCharger : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Pawn { Spawned: not false } pawn2) || !pawn2.RaceProps.IsMechanoid || !pawn2.IsColonyMech)
		{
			return false;
		}
		CompDrone compDrone = pawn2.TryGetComp<CompDrone>();
		if (compDrone == null)
		{
			return false;
		}
		if (pawn2.Downed || !compDrone.IsSelfShutdown || compDrone.workMode == AncotDefOf.Ancot_SelfShutdown)
		{
			return false;
		}
		if (compDrone.PercentFull > compDrone.PercentRecharge)
		{
			return false;
		}
		if (pawn2.CurJobDef == AncotJobDefOf.Ancot_DroneCharge)
		{
			return false;
		}
		if (!pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		return JobGiver_GetDroneEnergy_Charger.GetClosestCharger(pawn2, pawn, forced) != null;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Pawn pawn2 = (Pawn)t;
		Building_DroneCharger closestCharger = JobGiver_GetDroneEnergy_Charger.GetClosestCharger(pawn2, pawn, forced);
		Job job = JobMaker.MakeJob(AncotJobDefOf.Ancot_HaulDroneToCharger, pawn2, closestCharger, closestCharger.InteractionCell);
		job.count = 1;
		return job;
	}
}
