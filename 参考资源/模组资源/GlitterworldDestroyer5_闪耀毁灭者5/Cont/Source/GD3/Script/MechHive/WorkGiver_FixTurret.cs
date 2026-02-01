using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace GD3
{
	public class WorkGiver_FixTurret : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
			for (int i = 0; i < allBuildingsColonist.Count; i++)
			{
				Building_BrokenTurret building = allBuildingsColonist[i] as Building_BrokenTurret;
				if (building != null)
				{
					return false;
				}
			}
			return true;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.Faction != pawn.Faction)
			{
				return false;
			}
			if (!(t is Building_BrokenTurret building))
			{
				return false;
			}
			if (building.IsForbidden(pawn))
			{
				return false;
			}
			if (!pawn.CanReserve(building, 1, -1, null, forced))
			{
				return false;
			}
			if (!building.shouldBeNoticed)
			{
				return false;
			}
			if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
			{
				return false;
			}
			if (building.IsBurning())
			{
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(GDDefOf.GD_FixTurret, t, 1500, checkOverrideOnExpiry: true);
		}
	}
}
