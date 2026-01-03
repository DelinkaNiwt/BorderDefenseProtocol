using Verse;

namespace Milira;

public class CompProperties_GainHediffSpawn : CompProperties
{
	public HediffDef hediffDef;

	public CompProperties_GainHediffSpawn()
	{
		compClass = typeof(CompGainHediffSpawn);
	}
}
