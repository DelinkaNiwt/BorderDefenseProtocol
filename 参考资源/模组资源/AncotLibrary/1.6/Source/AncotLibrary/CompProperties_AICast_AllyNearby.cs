using RimWorld;

namespace AncotLibrary;

public class CompProperties_AICast_AllyNearby : CompProperties_AbilityEffect
{
	public float maxDistance = 2f;

	public int minCloseAlly = 2;

	public bool mechOnly = false;

	public bool fleshOnly = false;

	public int regionsToScan = 9;

	public CompProperties_AICast_AllyNearby()
	{
		compClass = typeof(CompAbilityAICast_AllyNearby);
	}
}
