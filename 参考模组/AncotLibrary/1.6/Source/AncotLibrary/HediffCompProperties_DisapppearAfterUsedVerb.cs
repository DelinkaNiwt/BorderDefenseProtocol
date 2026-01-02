using Verse;

namespace AncotLibrary;

public class HediffCompProperties_DisapppearAfterUsedVerb : HediffCompProperties
{
	public int delayTicks = 0;

	public EffecterDef disapppearEffecter;

	public HediffCompProperties_DisapppearAfterUsedVerb()
	{
		compClass = typeof(HediffComp_DisapppearAfterUsedVerb);
	}
}
