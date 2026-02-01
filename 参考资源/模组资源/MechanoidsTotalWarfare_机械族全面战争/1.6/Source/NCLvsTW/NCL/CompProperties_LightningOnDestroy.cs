using Verse;

namespace NCL;

public class CompProperties_LightningOnDestroy : CompProperties
{
	public int empRadius = 4;

	public DamageDef damageType;

	public int maxTargets = 3;

	public int damageAmount = 50;

	public float strikeRange = 200f;

	public ThingDef mechWormDef;

	public CompProperties_LightningOnDestroy()
	{
		compClass = typeof(CompLightningOnDestroy);
	}
}
