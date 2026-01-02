using RimWorld;

namespace AncotLibrary;

public class Building_TurretGunForceAiming : Building_TurretGun
{
	protected override bool CanSetForcedTarget => true;
}
