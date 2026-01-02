using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompSmartWeapon : ThingComp
{
	private Verb verb;

	private CompEquippable compEquippable;

	public CompPreperties_SmartWeapon Props => (CompPreperties_SmartWeapon)props;

	private CompEquippable EquipmentSource
	{
		get
		{
			if (compEquippable != null)
			{
				return compEquippable;
			}
			compEquippable = parent.TryGetComp<CompEquippable>();
			if (compEquippable == null)
			{
				Log.ErrorOnce(parent.LabelCap + " Comp_SmartWeapon but no CompEquippable", 50020);
			}
			return compEquippable;
		}
	}

	private Verb get_Verb()
	{
		if (verb == null)
		{
			verb = EquipmentSource.PrimaryVerb;
		}
		return verb;
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Stat_CMCDMGDrange_Label".Translate(), "Stat_CMCDMGDrange_Desc".Translate(Props.DamageDeductionRange), "Stat_CMCDMGDrange_Text".Translate(Props.DamageDeductionRange), 101);
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatMinDamageMultiplier_Label".Translate(), "StatMinDamageMultiplier_Desc".Translate(Props.MinDamageMultiplier.ToStringPercent()), "StatMinDamageMultiplier_Text".Translate(Props.MinDamageMultiplier.ToStringPercent()), 102);
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatMinPeneMultiplier_Label".Translate(), "StatMinPeneMultiplier_Desc".Translate(Props.MinPenetrationMultiplier.ToStringPercent()), "StatMinPeneMultiplier_Text".Translate(Props.MinPenetrationMultiplier.ToStringPercent()), 103);
	}
}
