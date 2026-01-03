using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class WorkGiver_EmptySunLightFuelContainer : WorkGiver_Scanner
{
	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.listerThings.AllThings.Where((Thing t) => t.def.defName == "Milira_SunLightGatheringTower").ToList();
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return pawn.Map.listerThings.AllThings.Where((Thing t) => t.def.defName == "Milira_SunLightGatheringTower").ToList().NullOrEmpty();
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t.IsForbidden(pawn))
		{
			return false;
		}
		LocalTargetInfo target = t;
		bool ignoreOtherReservations = forced;
		if (!pawn.CanReserve(target, 1, -1, GetReservationLayer(pawn, t), ignoreOtherReservations))
		{
			return false;
		}
		CompGenerator_SunLightFuel compGenerator_SunLightFuel = t.TryGetComp<CompGenerator_SunLightFuel>();
		if (compGenerator_SunLightFuel == null || !compGenerator_SunLightFuel.CanEmptyNow)
		{
			return false;
		}
		if (!StoreUtility.TryFindBestBetterStorageFor(compGenerator_SunLightFuel.SunLightFuel, pawn, pawn.Map, StoragePriority.Unstored, pawn.Faction, out var _, out var _, needAccurateResult: false))
		{
			JobFailReason.Is(HaulAIUtility.NoEmptyPlaceLowerTrans);
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		CompGenerator_SunLightFuel compGenerator_SunLightFuel = t.TryGetComp<CompGenerator_SunLightFuel>();
		if (compGenerator_SunLightFuel == null || !compGenerator_SunLightFuel.CanEmptyNow)
		{
			return null;
		}
		if (!StoreUtility.TryFindBestBetterStorageFor(compGenerator_SunLightFuel.SunLightFuel, pawn, pawn.Map, StoragePriority.Unstored, pawn.Faction, out var foundCell, out var _))
		{
			JobFailReason.Is(HaulAIUtility.NoEmptyPlaceLowerTrans);
			return null;
		}
		Job job = JobMaker.MakeJob(MiliraDefOf.Milira_EmptySunLightFuelContainer, t, compGenerator_SunLightFuel.SunLightFuel, foundCell);
		job.count = compGenerator_SunLightFuel.SunLightFuel.stackCount;
		return job;
	}

	public override ReservationLayerDef GetReservationLayer(Pawn pawn, LocalTargetInfo t)
	{
		return ReservationLayerDefOf.Empty;
	}
}
