using RimWorld;

namespace Milira;

public class CompProperties_Spawner_SunLightReactor : CompProperties_Spawner
{
	public float generationExplosionProbability;

	public bool requiresFuel;

	public CompProperties_Spawner_SunLightReactor()
	{
		compClass = typeof(CompSpawner_SunLightReactor);
	}
}
