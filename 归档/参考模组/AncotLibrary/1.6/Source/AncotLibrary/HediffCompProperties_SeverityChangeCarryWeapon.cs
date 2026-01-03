using Verse;

namespace AncotLibrary;

public class HediffCompProperties_SeverityChangeCarryWeapon : HediffCompProperties
{
	public float severityCarryWeapon = 0.01f;

	public float severityDefault = 0.1f;

	public int intervalTicks = 120;

	public HediffCompProperties_SeverityChangeCarryWeapon()
	{
		compClass = typeof(HediffComp_SeverityChangeCarryWeapon);
	}
}
