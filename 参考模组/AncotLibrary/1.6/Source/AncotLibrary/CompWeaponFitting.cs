using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompWeaponFitting : ThingComp
{
	public CompProperties_WeaponFittings Props => (CompProperties_WeaponFittings)props;

	public void UseWeaponFitting(ThingWithComps weapon, Pawn pawn)
	{
		WeaponTraitsUtility.AddOrReplaceTrait(Props.trait, weapon, out var replacedTraits, pawn);
		if (replacedTraits.NullOrEmpty())
		{
			return;
		}
		foreach (WeaponTraitDef item in replacedTraits)
		{
			WeaponTraitsUtility.DropWeaponFitting(item, pawn.Position, pawn.Map);
		}
	}
}
