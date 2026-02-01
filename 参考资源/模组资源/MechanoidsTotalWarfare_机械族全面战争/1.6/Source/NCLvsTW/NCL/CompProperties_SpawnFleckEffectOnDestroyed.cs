using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_SpawnFleckEffectOnDestroyed : CompProperties
{
	public List<FleckData> flecks;

	public List<EffectData> effects;

	public float globalScaleFactor = 1f;

	public CompProperties_SpawnFleckEffectOnDestroyed()
	{
		compClass = typeof(CompSpawnFleckEffectOnDestroyed);
	}
}
