using Verse;

namespace AncotLibrary;

public class HediffCompProperties_Heal : HediffCompProperties
{
	public int intervalTicks = 60;

	public EffecterDef effectDef;

	public HediffCompProperties_Heal()
	{
		compClass = typeof(HediffComp_Heal);
	}
}
