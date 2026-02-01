using Verse;

namespace NCL;

public class CompProperties_TameableMech : CompProperties
{
	public float baseTameChance = 0.5f;

	public int componentsPerTame = 1;

	public CompProperties_TameableMech()
	{
		compClass = typeof(CompTameableMech);
	}
}
