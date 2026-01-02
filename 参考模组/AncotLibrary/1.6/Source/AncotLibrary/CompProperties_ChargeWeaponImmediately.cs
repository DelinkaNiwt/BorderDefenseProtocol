using Verse;

namespace AncotLibrary;

public class CompProperties_ChargeWeaponImmediately : CompProperties
{
	public float chargeCooldownTime = 300f;

	public CompProperties_ChargeWeaponImmediately()
	{
		compClass = typeof(CompChargeWeaponImmediately);
	}
}
