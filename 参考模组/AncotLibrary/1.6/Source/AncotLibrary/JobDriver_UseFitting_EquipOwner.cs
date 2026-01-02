using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_UseFitting_EquipOwner : JobDriver_DestroyItem
{
	public ThingWithComps Weapon => job.GetTarget(TargetIndex.B).Thing as ThingWithComps;

	public override void Destroy()
	{
		CompWeaponFitting compWeaponFitting = base.TargetItem?.TryGetComp<CompWeaponFitting>();
		if (Weapon != null)
		{
			compWeaponFitting?.UseWeaponFitting(Weapon, pawn);
		}
		if (base.TargetItem.stackCount > 1)
		{
			base.TargetItem.stackCount--;
		}
		else
		{
			base.Destroy();
		}
	}
}
