using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_DisassembleBroadShieldUnit : JobDriver
{
	private float workLeft;

	private float totalNeededWork;

	protected Thing Target => job.targetA.Thing;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref workLeft, "workLeft", 0f);
		Scribe_Values.Look(ref totalNeededWork, "totalNeededWork", 0f);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOn(() => !Target.Faction.HostileTo(pawn.Faction));
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		this.FailOn(() => Target.def != MiliraDefOf.Milian_BroadShieldUnit);
		Toil doWork = ToilMaker.MakeToil("MakeNewToils").FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		doWork.initAction = delegate
		{
			totalNeededWork = 300f;
			workLeft = totalNeededWork;
		};
		doWork.handlingFacing = true;
		doWork.tickAction = delegate
		{
			workLeft -= 1f;
			if (workLeft <= 0f)
			{
				doWork.actor.jobs.curDriver.ReadyForNextToil();
			}
		};
		doWork.defaultCompleteMode = ToilCompleteMode.Never;
		doWork.WithProgressBar(TargetIndex.A, () => 1f - workLeft / totalNeededWork);
		yield return doWork;
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			if (Target.Faction != null)
			{
				Target.Faction.Notify_BuildingRemoved((Building)Target.GetInnerIfMinified(), pawn);
			}
			FinishedRemoving();
			base.Map.designationManager.RemoveAllDesignationsOn(Target);
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return toil;
	}

	protected virtual void FinishedRemoving()
	{
		Target.Destroy(DestroyMode.Deconstruct);
		pawn.records.Increment(RecordDefOf.ThingsDeconstructed);
		Ability ability = pawn.abilities.abilities.FirstOrDefault((Ability a) => a.def.defName == "Milian_BroadShieldAssist");
		ability.StartCooldown(0);
		CompThingCarrier_Custom val = ((Thing)pawn).TryGetComp<CompThingCarrier_Custom>();
		Thing thing = ThingMaker.MakeThing(val.fixedIngredient);
		int count = (thing.stackCount = 200);
		val.innerContainer.TryAdd(thing, count);
	}
}
