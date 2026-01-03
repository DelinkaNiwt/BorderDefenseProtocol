using Verse;

namespace AncotLibrary;

public class HediffCompProperties_Moving : HediffCompProperties
{
	public float severityMoving = 1f;

	public float severityDefault = 0.1f;

	public float minSpeed = 1f;

	public bool onlyInCombat = false;

	public bool useDefaultSeverity = true;

	public HediffCompProperties_Moving()
	{
		compClass = typeof(HediffComp_Moving);
	}
}
