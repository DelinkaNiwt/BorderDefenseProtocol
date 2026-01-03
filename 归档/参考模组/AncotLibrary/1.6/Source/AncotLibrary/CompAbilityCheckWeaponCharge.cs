using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityCheckWeaponCharge : CompAbilityEffect
{
	public new CompProperties_AbilityCheckWeaponCharge Props => (CompProperties_AbilityCheckWeaponCharge)props;

	private CompWeaponCharge CompCharge => parent.pawn.equipment.Primary.TryGetComp<CompWeaponCharge>();

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (CompCharge == null || !CompCharge.CanBeUsed)
		{
			return false;
		}
		return true;
	}

	public override bool GizmoDisabled(out string reason)
	{
		if (CompCharge == null || !CompCharge.CanBeUsed)
		{
			reason = "Ancot.NoWeaponAbilityCharge".Translate();
			return true;
		}
		reason = "";
		return false;
	}
}
