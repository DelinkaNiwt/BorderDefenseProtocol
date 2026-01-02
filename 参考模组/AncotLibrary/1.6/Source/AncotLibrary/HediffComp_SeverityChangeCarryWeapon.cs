using Verse;

namespace AncotLibrary;

public class HediffComp_SeverityChangeCarryWeapon : HediffComp
{
	private HediffCompProperties_SeverityChangeCarryWeapon Props => (HediffCompProperties_SeverityChangeCarryWeapon)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(Props.intervalTicks, delta))
		{
			if (base.Pawn.equipment.Primary != null)
			{
				parent.Severity = Props.severityCarryWeapon;
			}
			else
			{
				parent.Severity = Props.severityDefault;
			}
		}
	}
}
