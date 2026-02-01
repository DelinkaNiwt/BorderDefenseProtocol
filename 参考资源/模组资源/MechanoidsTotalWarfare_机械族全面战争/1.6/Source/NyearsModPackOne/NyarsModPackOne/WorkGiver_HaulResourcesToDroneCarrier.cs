using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NyarsModPackOne;

public class WorkGiver_HaulResourcesToDroneCarrier : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Pawn { Spawned: not false, Downed: false } pawn2))
		{
			return false;
		}
		CompDroneCarrier compDroneCarrier = pawn2.TryGetComp<CompDroneCarrier>();
		if (compDroneCarrier == null)
		{
			return false;
		}
		int amountToAutofill = compDroneCarrier.AmountToAutofill;
		return amountToAutofill > 0 && pawn.CanReserve(t, 1, -1, null, forced) && !HaulAIUtility.FindFixedIngredientCount(pawn, compDroneCarrier.Props.fixedIngredient, amountToAutofill).NullOrEmpty();
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		CompDroneCarrier compDroneCarrier = t.TryGetComp<CompDroneCarrier>();
		if (compDroneCarrier == null)
		{
			return null;
		}
		int amountToAutofill = compDroneCarrier.AmountToAutofill;
		if (amountToAutofill <= 0)
		{
			return null;
		}
		List<Thing> list = HaulAIUtility.FindFixedIngredientCount(pawn, compDroneCarrier.Props.fixedIngredient, amountToAutofill);
		if (list.NullOrEmpty())
		{
			return null;
		}
		Job job = HaulAIUtility.HaulToContainerJob(pawn, list[0], t);
		job.count = Mathf.Min(job.count, amountToAutofill);
		if (list.Count > 1)
		{
			job.targetQueueB = (from res in list.Skip(1)
				select new LocalTargetInfo(res)).ToList();
		}
		return job;
	}
}
