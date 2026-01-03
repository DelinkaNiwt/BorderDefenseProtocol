using Verse;

namespace AncotLibrary;

public class CompProperties_RangeWeaponVerbSwitch_EnergyPassive : CompProperties
{
	public VerbProperties verbProps = new VerbProperties();

	public CompProperties_RangeWeaponVerbSwitch_EnergyPassive()
	{
		compClass = typeof(CompRangeWeaponVerbSwitch_EnergyPassive);
	}
}
