using Verse;

namespace HNGT;

public class ModExtension_HighOrbitAttack : DefModExtension
{
	public string projectileDefName;

	public float impactAreaRadius = 15f;

	public int explosionCount = 30;

	public int bombIntervalTicks = 18;

	public int warmupTicks = 60;

	public string projectileTexturePath;

	public int projectileFlyTimeTicks = 60;

	public float preImpactSoundVolume = 1f;
}
