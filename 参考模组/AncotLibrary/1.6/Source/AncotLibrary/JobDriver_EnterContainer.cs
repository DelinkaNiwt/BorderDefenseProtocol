using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_EnterContainer : JobDriver
{
	private TargetIndex ContainerInd = TargetIndex.A;

	public CompThingContainer Container => job.GetTarget(ContainerInd).Thing?.TryGetComp<CompThingContainer>();

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(ContainerInd);
		this.FailOn(() => Container.Full);
		yield return Toils_Goto.GotoThing(ContainerInd, PathEndMode.InteractionCell);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			CompThingContainer container = Container;
			bool flag = pawn.DeSpawnOrDeselect();
			container.GetDirectlyHeldThings().TryAdd(pawn);
			if (flag)
			{
				Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
			}
		};
		yield return toil;
	}
}
