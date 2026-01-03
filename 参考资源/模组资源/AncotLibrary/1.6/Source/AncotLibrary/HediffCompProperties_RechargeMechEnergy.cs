using Verse;

namespace AncotLibrary;

public class HediffCompProperties_RechargeMechEnergy : HediffCompProperties
{
	public float energyPerCharge = 0.01f;

	public int intervalTicks = 600;

	public bool onlyDormant = false;

	public HediffCompProperties_RechargeMechEnergy()
	{
		compClass = typeof(HediffComp_RechargeMechEnergy);
	}
}
