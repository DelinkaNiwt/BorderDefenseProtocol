using Verse;

namespace NCL;

public class HediffCompProperties_DisappearTrigger : HediffCompProperties
{
	public HediffDef hediffToGive;

	public bool onlyIfFullyHealed = false;

	public HediffCompProperties_DisappearTrigger()
	{
		compClass = typeof(HediffComp_DisappearTrigger);
	}
}
