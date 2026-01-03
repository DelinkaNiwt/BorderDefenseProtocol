using Verse;

namespace AncotLibrary;

public class HediffCompProperties_PhysicalShieldState_Severity : HediffCompProperties
{
	public float severityActive = 1f;

	public float severityReady = 1f;

	public float severityDisable = 1f;

	public float severityResetting = 1f;

	public HediffCompProperties_PhysicalShieldState_Severity()
	{
		compClass = typeof(HediffComp_PhysicalShieldState_Severity);
	}
}
