using Verse;

namespace Milira;

public class HediffCompProperties_Cameleoline : HediffCompProperties
{
	public float recoverTick = 240f;

	public EffecterDef effecter;

	public HediffCompProperties_Cameleoline()
	{
		compClass = typeof(HediffComp_Cameleoline);
	}
}
