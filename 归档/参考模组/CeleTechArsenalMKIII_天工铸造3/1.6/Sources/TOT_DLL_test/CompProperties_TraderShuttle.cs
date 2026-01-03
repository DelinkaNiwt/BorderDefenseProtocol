using Verse;

namespace TOT_DLL_test;

public class CompProperties_TraderShuttle : CompProperties
{
	public SoundDef soundThud;

	public ThingDef landAnimation;

	public ThingDef takeoffAnimation;

	public CompProperties_TraderShuttle()
	{
		compClass = typeof(Comp_TraderShuttle);
	}
}
