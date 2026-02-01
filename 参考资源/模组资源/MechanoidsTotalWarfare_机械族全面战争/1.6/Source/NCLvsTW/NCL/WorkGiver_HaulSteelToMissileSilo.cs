using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class WorkGiver_HaulSteelToMissileSilo : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("NCL_Building_MissileSilo"));

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (pawn == null || t == null || !t.Spawned || t.IsBurning())
		{
			return false;
		}
		CompSteelResource comp = t.TryGetComp<CompSteelResource>();
		if (comp == null || !comp.AutoFill)
		{
			return false;
		}
		int amount = comp.AmountToAutofill;
		if (amount <= 0)
		{
			return false;
		}
		if (!pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		ThingDef steelDef = comp.Props.fixedIngredient;
		Predicate<Thing> validator = delegate(Thing x)
		{
			if (x == null || x.IsForbidden(pawn) || x.IsBurning())
			{
				return false;
			}
			return pawn.CanReserve(x, 1, -1, null, forced) ? true : false;
		};
		Thing nearest = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(steelDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
		return nearest != null;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		CompSteelResource comp = t.TryGetComp<CompSteelResource>();
		if (comp == null)
		{
			return null;
		}
		int amount = comp.AmountToAutofill;
		if (amount <= 0)
		{
			return null;
		}
		List<Thing> resources = HaulAIUtility.FindFixedIngredientCount(pawn, comp.Props.fixedIngredient, amount);
		if (resources.NullOrEmpty())
		{
			return null;
		}
		if (!pawn.CanReserve(resources[0], 1, -1, null, forced))
		{
			return null;
		}
		if (!pawn.CanReach(resources[0], PathEndMode.ClosestTouch, Danger.Deadly))
		{
			return null;
		}
		Job job = HaulAIUtility.HaulToContainerJob(pawn, resources[0], t);
		job.count = Mathf.Min(job.count, amount);
		if (resources.Count > 1)
		{
			job.targetQueueB = new List<LocalTargetInfo>();
			for (int i = 1; i < resources.Count; i++)
			{
				job.targetQueueB.Add(resources[i]);
			}
		}
		return job;
	}

	private List<Thing> FindResources(Pawn pawn, ThingDef resourceDef, int amountNeeded)
	{
		return HaulAIUtility.FindFixedIngredientCount(pawn, resourceDef, amountNeeded);
	}
}
