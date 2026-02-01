using RimWorld;
using Verse;

namespace Edited_BM_WeaponSummon;

public class CompProperties_SummonWeapon : CompProperties_AbilityEffect
{
	public ThingDef weapon;

	public CompProperties_SummonWeapon()
	{
		compClass = typeof(CompSummonWeapon);
	}
}
