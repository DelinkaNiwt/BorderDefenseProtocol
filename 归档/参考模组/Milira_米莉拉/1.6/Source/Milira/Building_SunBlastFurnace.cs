using RimWorld;
using Verse;

namespace Milira;

public class Building_SunBlastFurnace : Building_WorkTable
{
	private CompSunBlastFurnaceOutdoorBreakdown furnaceOutdoorBreakdownComp;

	private CompSunBlasterFurnaceHeatPusher sunBlasterFurnaceHeatPusherComp;

	private CompSunBlastFurnaceIllegalUse sunBlastFurnaceIllegalUseComp;

	private CompRefuelable refuelComp => GetComp<CompRefuelable>();

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		furnaceOutdoorBreakdownComp = GetComp<CompSunBlastFurnaceOutdoorBreakdown>();
		sunBlasterFurnaceHeatPusherComp = GetComp<CompSunBlasterFurnaceHeatPusher>();
		sunBlastFurnaceIllegalUseComp = GetComp<CompSunBlastFurnaceIllegalUse>();
	}

	public override void UsedThisTick()
	{
		base.UsedThisTick();
		if (sunBlasterFurnaceHeatPusherComp != null && base.Map != null)
		{
			sunBlasterFurnaceHeatPusherComp.HeatPush();
		}
		if (furnaceOutdoorBreakdownComp != null && base.Position.UsesOutdoorTemperature(base.Map) && refuelComp.Fuel != 0f)
		{
			furnaceOutdoorBreakdownComp.Notify_UsedThisTick();
		}
		if (sunBlastFurnaceIllegalUseComp != null && base.Map != null)
		{
			sunBlastFurnaceIllegalUseComp.Notify_UsedThisTick();
		}
	}
}
