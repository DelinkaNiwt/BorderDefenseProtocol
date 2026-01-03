using Verse;

namespace AncotLibrary;

public class HediffCompProperties_DecreaseAfterUsedVerb : HediffCompProperties
{
	public float minSeverity = 0.01f;

	public float severityPerUse = 0.01f;

	public bool verbShootOnly;

	public HediffCompProperties_DecreaseAfterUsedVerb()
	{
		compClass = typeof(HediffComp_DecreaseAfterUsedVerb);
	}
}
