using Verse;

namespace AncotLibrary;

public class HediffCompProperties_EffecterMaintain : HediffCompProperties
{
	public EffecterDef effcterDef;

	public int maintainTicks = 100;

	public float scale = 1f;

	public HediffCompProperties_EffecterMaintain()
	{
		compClass = typeof(HediffComp_EffecterMaintain);
	}
}
