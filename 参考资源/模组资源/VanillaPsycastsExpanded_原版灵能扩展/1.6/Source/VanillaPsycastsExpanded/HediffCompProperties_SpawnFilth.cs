using Verse;

namespace VanillaPsycastsExpanded;

public class HediffCompProperties_SpawnFilth : HediffCompProperties
{
	public ThingDef filthDef;

	public int intervalRate;

	public IntRange filthCount;

	public HediffCompProperties_SpawnFilth()
	{
		compClass = typeof(HediffComp_SpawnFilth);
	}
}
