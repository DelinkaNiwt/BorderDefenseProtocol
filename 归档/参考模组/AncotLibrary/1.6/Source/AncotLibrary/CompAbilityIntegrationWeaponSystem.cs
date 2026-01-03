using Verse;

namespace AncotLibrary;

public class CompAbilityIntegrationWeaponSystem : CompAbilityCheckApparelReloadable
{
	public CompIntegrationWeaponSystem CompIWS => base.Apparel?.TryGetComp<CompIntegrationWeaponSystem>();

	public override bool ShouldHideGizmo => !CompIWS.activate;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return CompIWS.activate;
	}
}
