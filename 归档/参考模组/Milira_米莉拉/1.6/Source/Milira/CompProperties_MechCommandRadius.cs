using Verse;

namespace Milira;

public class CompProperties_MechCommandRadius : CompProperties
{
	public float mechCommandRadius = 1f;

	public CompProperties_MechCommandRadius()
	{
		compClass = typeof(CompMechCommandRadius);
	}
}
