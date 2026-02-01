using Verse;

namespace NCL;

public class CompProperties_FragmentedExplosive : CompProperties
{
	public float explosionRadius = 5f;

	public int fragmentCount = 10;

	public float guaranteedCenterFraction = 0.5f;

	public ThingDef fragmentProjectileDef;

	public float fireGlowSize = 3.5f;

	public float smokeSize = 5.5f;

	public float heatGlowSize = 3.5f;

	public CompProperties_FragmentedExplosive()
	{
		compClass = typeof(Comp_FragmentedExplosive);
	}
}
