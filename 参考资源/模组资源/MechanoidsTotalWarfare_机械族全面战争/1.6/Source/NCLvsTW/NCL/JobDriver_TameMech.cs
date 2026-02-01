using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class JobDriver_TameMech : JobDriver
{
	private const TargetIndex MechIndex = TargetIndex.A;

	private const TargetIndex ComponentIndex = TargetIndex.B;

	protected Pawn Mech => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Mech, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOn(() => Mech.Faction != null || !Mech.RaceProps.IsMechanoid);
		yield return new Toil
		{
			initAction = delegate
			{
				int num = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);
				if (num >= job.count)
				{
					ReadyForNextToil();
				}
			}
		};
		yield return new Toil
		{
			initAction = delegate
			{
				Thing thing = FindClosestComponents(pawn, job.count - pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial));
				if (thing == null)
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
				else
				{
					job.SetTarget(TargetIndex.B, thing);
				}
			}
		};
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
		yield return Toils_Haul.TakeToInventory(TargetIndex.B, job.count - pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial));
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		yield return new Toil
		{
			initAction = delegate
			{
				pawn.pather.StopDead();
				pawn.rotationTracker.FaceTarget(Mech);
			},
			defaultCompleteMode = ToilCompleteMode.Delay,
			defaultDuration = 250
		};
		yield return new Toil
		{
			initAction = delegate
			{
				int componentsPerTame = Mech.GetComp<CompTameableMech>().Props.componentsPerTame;
				List<Thing> list = pawn.inventory.innerContainer.Where((Thing t) => t.def == ThingDefOf.ComponentIndustrial).ToList();
				int num = componentsPerTame;
				foreach (Thing current in list)
				{
					int num2 = Mathf.Min(num, current.stackCount);
					pawn.inventory.innerContainer.Take(current, num2).Destroy();
					num -= num2;
					if (num <= 0)
					{
						break;
					}
				}
				if (Rand.Value < 0.5f)
				{
					Mech.SetFaction(Faction.OfPlayer);
					Messages.Message("MessageMechTamed".Translate(Mech.LabelShort), Mech, MessageTypeDefOf.PositiveEvent);
				}
				else
				{
					Messages.Message("MessageFailedToTameMech".Translate(Mech.LabelShort), Mech, MessageTypeDefOf.NegativeEvent);
					Job job = JobMaker.MakeJob(TameMechefOf.TW_TameMech, Mech);
					job.count = componentsPerTame;
					pawn.jobs.jobQueue.EnqueueFirst(job);
					if (pawn.Map.designationManager.DesignationOn(Mech, TameMechefOf.TW_TameMechDesignation) == null)
					{
						pawn.Map.designationManager.AddDesignation(new Designation(Mech, TameMechefOf.TW_TameMechDesignation));
					}
				}
			},
			defaultCompleteMode = ToilCompleteMode.Instant
		};
	}

	private Thing FindClosestComponents(Pawn pawn, int needed)
	{
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.ComponentIndustrial), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 50f, (Thing t) => !t.IsForbidden(pawn) && pawn.CanReserve(t) && t.stackCount >= needed);
	}
}
