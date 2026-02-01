using Verse;

namespace NCL;

public class CompProperties_SpawnEffectOnDestroy : CompProperties
{
	public string effectDefName;

	public float effectSize = 1f;

	public int durationTicks = 60;

	public CompProperties_SpawnEffectOnDestroy()
	{
		compClass = typeof(Comp_SpawnEffectOnDestroy);
	}
}
