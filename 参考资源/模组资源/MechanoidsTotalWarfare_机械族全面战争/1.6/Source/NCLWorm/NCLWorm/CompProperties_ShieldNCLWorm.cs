using Verse;

namespace NCLWorm;

public class CompProperties_ShieldNCLWorm : CompProperties
{
	public int RestTime = 60;

	public float minDrawSize = 1.2f;

	public float maxDrawSize = 1.55f;

	public float EnergyShieldEnergyMax = 100f;

	public float EnergyShieldRechargeRate = 1f;

	public float energyOnReset = 1f;

	public CompProperties_ShieldNCLWorm()
	{
		compClass = typeof(CompShieldNCLWorm);
	}
}
