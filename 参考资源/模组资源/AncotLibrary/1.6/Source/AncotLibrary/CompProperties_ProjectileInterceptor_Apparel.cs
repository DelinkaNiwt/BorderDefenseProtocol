using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_ProjectileInterceptor_Apparel : CompProperties
{
	public ThingDef mechShieldType = ThingDefOf.MechShield;

	public int hitPointBase = 100;

	public CompProperties_ProjectileInterceptor_Apparel()
	{
		compClass = typeof(CompProjectileInterceptor_Apparel);
	}
}
