using Verse;

namespace NCL;

public class CompProperties_DualHediffMechHealer : CompProperties
{
	public float radius = 10f;

	public int healIntervalTicks = 600;

	public HediffDef hediffOne;

	public string texPathOne = "Things/None";

	public string labelOne = "Hediff_One";

	public HediffDef hediffTwo;

	public string texPathTwo = "Things/None";

	public string labelTwo = "Hediff_Two";

	public float fuelCostLight = 1f;

	public float fuelCostMedium = 2f;

	public float fuelCostHeavy = 3f;

	public float fuelCostUltraHeavy = 5f;

	public CompProperties_DualHediffMechHealer()
	{
		compClass = typeof(CompDualHediffMechHealer);
	}
}
