using RimWorld;
using Verse;

namespace Edited_BM_WeaponSummon;

public class CompSummonWeapon : CompAbilityEffect
{
	public new CompProperties_SummonWeapon Props => props as CompProperties_SummonWeapon;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		ThingWithComps primary = parent.pawn.equipment.Primary;
		if (primary != null)
		{
			parent.pawn.equipment.TryTransferEquipmentToContainer(primary, parent.pawn.inventory.innerContainer);
		}
		ThingWithComps thingWithComps = ThingMaker.MakeThing(Props.weapon) as ThingWithComps;
		thingWithComps.GetComp<CompSummonedWeapon>().ticksSummoned = Find.TickManager.TicksGame;
		parent.pawn.equipment.AddEquipment(thingWithComps);
	}
}
