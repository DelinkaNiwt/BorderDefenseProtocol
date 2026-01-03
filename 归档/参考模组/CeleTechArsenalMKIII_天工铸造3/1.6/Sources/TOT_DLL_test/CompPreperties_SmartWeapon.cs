using Verse;

namespace TOT_DLL_test;

public class CompPreperties_SmartWeapon : CompProperties
{
	public int DamageDeductionRange = 5;

	public float MinDamageMultiplier = 0.5f;

	public float MinPenetrationMultiplier = 0.13f;

	public CompPreperties_SmartWeapon()
	{
		compClass = typeof(CompSmartWeapon);
	}
}
