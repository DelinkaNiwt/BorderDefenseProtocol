using Verse;

namespace AncotLibrary;

public class CompProperties_ProjectileInterceptor_Enhance : CompProperties
{
	public int hitPointBase = 100;

	public float factorAwful = 1f;

	public float factorPoor = 1f;

	public float factorNormal = 1f;

	public float factorGood = 1f;

	public float factorExcellent = 1f;

	public float factorMasterwork = 1f;

	public float factorLegendary = 1f;

	public CompProperties_ProjectileInterceptor_Enhance()
	{
		compClass = typeof(CompProjectileInterceptor_Enhance);
	}
}
