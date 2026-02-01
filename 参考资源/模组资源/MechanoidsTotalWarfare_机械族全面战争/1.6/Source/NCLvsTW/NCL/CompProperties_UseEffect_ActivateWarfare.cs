using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_UseEffect_ActivateWarfare : CompProperties_UseEffect
{
	public int delayTicks = -1;

	public EffecterDef activateEffect;

	public CompProperties_UseEffect_ActivateWarfare()
	{
		compClass = typeof(CompUseEffect_ActivateWarfare);
	}
}
