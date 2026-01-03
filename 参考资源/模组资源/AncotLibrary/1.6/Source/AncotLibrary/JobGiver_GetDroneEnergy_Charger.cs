using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_GetDroneEnergy_Charger : JobGiver_GetDroneEnergy
{
	public static Building_DroneCharger GetClosestCharger(Pawn mech, Pawn carrier, bool forced)
	{
		if (!mech.Spawned || !carrier.Spawned)
		{
			return null;
		}
		Danger danger = (forced ? Danger.Deadly : Danger.Some);
		return (Building_DroneCharger)GenClosest.ClosestThingReachable(mech.Position, mech.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.InteractionCell, TraverseParms.For(carrier, danger), 9999f, delegate(Thing t)
		{
			if (!(t is Building_DroneCharger building_DroneCharger))
			{
				return false;
			}
			if (!carrier.CanReach(t, PathEndMode.InteractionCell, danger))
			{
				return false;
			}
			if (carrier != mech)
			{
				if (!forced && building_DroneCharger.Map.reservationManager.ReservedBy(building_DroneCharger, carrier))
				{
					return false;
				}
				if (forced && KeyBindingDefOf.QueueOrder.IsDownEvent && building_DroneCharger.Map.reservationManager.ReservedBy(building_DroneCharger, carrier))
				{
					return false;
				}
			}
			return !t.IsForbidden(carrier) && carrier.CanReserve(t, 1, -1, null, forced) && building_DroneCharger.CanPawnChargeCurrently(mech);
		});
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (!ShouldAutoRecharge(pawn))
		{
			return null;
		}
		Building_DroneCharger closestCharger = GetClosestCharger(pawn, pawn, forced: false);
		if (closestCharger != null)
		{
			Job job = JobMaker.MakeJob(AncotJobDefOf.Ancot_DroneCharge, closestCharger);
			job.overrideFacing = Rot4.South;
			return job;
		}
		return null;
	}
}
