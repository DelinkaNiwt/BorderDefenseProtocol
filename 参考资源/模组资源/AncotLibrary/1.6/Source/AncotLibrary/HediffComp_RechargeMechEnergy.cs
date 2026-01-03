using Verse;

namespace AncotLibrary;

public class HediffComp_RechargeMechEnergy : HediffComp
{
	private HediffCompProperties_RechargeMechEnergy Props => (HediffCompProperties_RechargeMechEnergy)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(Props.intervalTicks, delta) && base.Pawn.needs.energy != null && Available())
		{
			base.Pawn.needs.energy.CurLevelPercentage += Props.energyPerCharge;
		}
	}

	public bool Available()
	{
		if (Props.onlyDormant)
		{
			return base.Pawn.CurJob.forceSleep;
		}
		return true;
	}
}
