using RimWorld;

namespace Milira;

public class CompSunBlasterFurnaceHeatPusherPowered : CompSunBlasterFurnaceHeatPusher
{
	protected CompPowerTrader powerComp;

	protected CompRefuelable refuelableComp;

	protected CompBreakdownable breakdownableComp;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		powerComp = parent.GetComp<CompPowerTrader>();
		refuelableComp = parent.GetComp<CompRefuelable>();
		breakdownableComp = parent.GetComp<CompBreakdownable>();
	}
}
