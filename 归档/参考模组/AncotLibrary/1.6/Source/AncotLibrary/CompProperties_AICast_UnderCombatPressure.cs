using RimWorld;

namespace AncotLibrary;

public class CompProperties_AICast_UnderCombatPressure : CompProperties_AbilityEffect
{
	public float maxThreatDistance = 2f;

	public int minCloseTargets = 2;

	public bool mechOnly = false;

	public bool fleshOnly = false;

	public int regionsToScan = 9;

	public CompProperties_AICast_UnderCombatPressure()
	{
		compClass = typeof(CompAbilityAICast_UnderCombatPressure);
	}
}
