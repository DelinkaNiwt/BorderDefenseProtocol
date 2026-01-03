using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_DisassembleWeaponForFitting : JobDriver_DestroyItem
{
	public ThingWithComps Weapon => job.GetTarget(TargetIndex.A).Thing as ThingWithComps;

	public override void Destroy()
	{
		CompUniqueWeapon compUniqueWeapon = base.TargetItem?.TryGetComp<CompUniqueWeapon>();
		if (Weapon != null && compUniqueWeapon != null)
		{
			WeaponTraitsUtility.RemoveAllTraitsAndDropFittings(base.TargetItem);
			base.Destroy();
		}
	}
}
