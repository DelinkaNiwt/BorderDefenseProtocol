using Verse;

namespace TOT_DLL_test;

public class CompProperties_FCradar : CompProperties
{
	public float rotatorSpeed = 0.2f;

	public CompProperties_FCradar()
	{
		compClass = typeof(Comp_FCradar);
	}
}
