using Verse;

namespace TOT_DLL_test;

public class CompProperties_WeaponGiveHediff_Sword : CompProperties
{
	public bool biocodeOnEquip;

	public CompProperties_WeaponGiveHediff_Sword()
	{
		compClass = typeof(CompWeaponGiveHediff_Sword);
	}
}
