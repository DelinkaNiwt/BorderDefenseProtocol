using Verse;

namespace TOT_DLL_test;

public class CompProperties_CMCShield : CompProperties
{
	public int startingTicksToReset = 1200;

	public float minDrawSize = 1.5f;

	public float maxDrawSize = 1.6f;

	public float energyLossPerDamage = 0.067f;

	public float energyOnReset = 0.2f;

	public bool blocksRangedWeapons = true;

	public CompProperties_CMCShield()
	{
		compClass = typeof(Comp_CMCShield);
	}
}
