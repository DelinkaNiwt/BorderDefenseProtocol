using Verse;

namespace AncotLibrary;

public class HediffCompProperties_MechRepair : HediffCompProperties
{
	public float energyPctPerRepair = 0f;

	public float energyThreshold = 0.1f;

	public int intervalTicks = 60;

	public EffecterDef effectDef;

	public HediffCompProperties_MechRepair()
	{
		compClass = typeof(HediffComp_MechRepair);
	}
}
