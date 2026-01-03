using RimWorld;

namespace TOT_DLL_test;

public class CompProperties_AntiInv : CompProperties_AbilityEffect
{
	public float SpotRange = 10f;

	public float DetectRange = 20f;

	public CompProperties_AntiInv()
	{
		compClass = typeof(CompProperties_AntiInv);
	}
}
