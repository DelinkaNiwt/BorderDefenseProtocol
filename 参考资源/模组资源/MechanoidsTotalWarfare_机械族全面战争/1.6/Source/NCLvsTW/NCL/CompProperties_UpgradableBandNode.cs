using RimWorld;

namespace NCL;

public class CompProperties_UpgradableBandNode : CompProperties_BandNode
{
	public int baseBandwidth = 1;

	public int bandwidthPerGeneration = 1;

	public int maxGenerations = 5;

	public float generationCooldownDays = 1f;

	public float extraPowerConsumptionPerGeneration = 50f;

	public CompProperties_UpgradableBandNode()
	{
		compClass = typeof(CompUpgradableBandNode);
	}
}
