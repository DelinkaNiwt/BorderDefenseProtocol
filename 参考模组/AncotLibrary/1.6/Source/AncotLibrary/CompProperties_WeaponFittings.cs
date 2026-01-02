using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_WeaponFittings : CompProperties
{
	public WeaponTraitDef trait;

	public int useDuration = 300;

	public CompProperties_WeaponFittings()
	{
		compClass = typeof(CompWeaponFitting);
	}
}
