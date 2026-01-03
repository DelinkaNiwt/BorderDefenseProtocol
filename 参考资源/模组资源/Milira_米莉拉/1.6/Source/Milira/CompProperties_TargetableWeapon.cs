using RimWorld;

namespace Milira;

public class CompProperties_TargetableWeapon : CompProperties_Targetable
{
	public bool shouldBeRangeWeapon = true;

	public CompProperties_TargetableWeapon()
	{
		compClass = typeof(CompTargetableWeapon);
	}
}
