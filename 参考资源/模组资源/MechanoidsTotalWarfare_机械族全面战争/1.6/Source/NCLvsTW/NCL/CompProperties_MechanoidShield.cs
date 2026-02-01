using Verse;

namespace NCL;

public class CompProperties_MechanoidShield : CompProperties
{
	public string reactivateMessageKey = "MechShield_Reactivated";

	public int checkIntervalTicks = 30;

	public CompProperties_MechanoidShield()
	{
		compClass = typeof(CompMechanoidShield);
	}
}
