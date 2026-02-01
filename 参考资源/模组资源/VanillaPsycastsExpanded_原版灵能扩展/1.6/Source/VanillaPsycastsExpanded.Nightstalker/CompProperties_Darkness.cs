using Verse;

namespace VanillaPsycastsExpanded.Nightstalker;

public class CompProperties_Darkness : CompProperties
{
	public float darknessRange;

	public CompProperties_Darkness()
	{
		compClass = typeof(CompDarkener);
	}
}
