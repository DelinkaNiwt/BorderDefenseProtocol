using Verse;

namespace NCL;

public class HediffCompProperties_ShieldGenerator : HediffCompProperties
{
	public float range;

	public HediffCompProperties_ShieldGenerator()
	{
		compClass = typeof(HediffComp_ShieldGenerator);
	}
}
