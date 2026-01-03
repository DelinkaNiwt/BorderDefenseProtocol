using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

internal class JobDriver_TradewithCMCTS : JobDriver
{
	private ThingWithComps Trader => base.TargetThingA as ThingWithComps;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Trader, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		Comp_TraderShuttle comp = Trader.TryGetComp<Comp_TraderShuttle>();
		this.FailOnDespawnedOrNull(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOn(() => comp == null || !comp.tradeShip.CanTradeNow);
		Toil trade = new Toil();
		trade.initAction = delegate
		{
			Pawn actor = trade.actor;
			if (comp.tradeShip.CanTradeNow)
			{
				Find.WindowStack.Add(new Dialog_Trade(actor, comp.tradeShip));
			}
		};
		yield return trade;
	}
}
