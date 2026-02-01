using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace GD3
{
    public class WorkGiver_CommunicationStation : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(GDDefOf.GD_CommunicationStation);

		public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
			for (int i = 0; i < allBuildingsColonist.Count; i++)
			{
				Building building = allBuildingsColonist[i];
				if (building.def == GDDefOf.GD_CommunicationStation)
				{
					CompPowerTrader comp = building.GetComp<CompPowerTrader>();
					if ((comp == null || comp.PowerOn) && building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) == null)
					{
						return false;
					}
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
			if (!(t is Building building))
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
			if (!building.TryGetComp<CompCommunicationStation>().CanOperateNow())
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
			return JobMaker.MakeJob(GDDefOf.GD_OperateStation, t, 1500, checkOverrideOnExpiry: true);
		}
	}
}
