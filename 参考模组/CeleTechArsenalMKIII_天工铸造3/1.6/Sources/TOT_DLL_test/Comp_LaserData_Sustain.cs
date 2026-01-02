using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Comp_LaserData_Sustain : ThingComp
{
	private float qualityNum;

	public CompProperties_LaserData_Sustain Props => (CompProperties_LaserData_Sustain)props;

	public float QualityNum
	{
		get
		{
			try
			{
				CompQuality compQuality = parent.TryGetComp<CompQuality>();
				switch (compQuality.Quality)
				{
				case QualityCategory.Awful:
					qualityNum = 0.9f;
					break;
				case QualityCategory.Poor:
				case QualityCategory.Normal:
				case QualityCategory.Good:
				case QualityCategory.Excellent:
					qualityNum = 1f;
					break;
				case QualityCategory.Masterwork:
					qualityNum = 1.25f;
					break;
				case QualityCategory.Legendary:
					qualityNum = 1.5f;
					break;
				}
				return qualityNum;
			}
			catch
			{
				return 1f;
			}
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		base.SpecialDisplayStats();
		IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
		if (enumerable != null)
		{
			foreach (StatDrawEntry item in enumerable)
			{
				yield return item;
			}
		}
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponDMG_Label".Translate(), "StatEnergyWeaponDMG_Desc".Translate((float)Props.DamageNum * QualityNum), "StatEnergyWeaponDMG_Text".Translate((float)Props.DamageNum * QualityNum), 114515);
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponAP_Label".Translate(), "StatEnergyWeaponAP_Desc".Translate(Props.DamageArmorPenetration.ToStringPercent()), "StatEnergyWeaponAP_Text".Translate(Props.DamageArmorPenetration.ToStringPercent()), 114514);
		if (Props.IfSecondDamage)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponDMG2_Label".Translate(), "StatEnergyWeaponDMG2_Desc".Translate(Props.DamageNum_B), "StatEnergyWeaponDMG2_Text".Translate(Props.DamageNum_B), 114513);
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponAP2_Label".Translate(), "StatEnergyWeaponAP2_Desc".Translate(Props.DamageArmorPenetration_B.ToStringPercent()), "StatEnergyWeaponAP2_Text".Translate(Props.DamageArmorPenetration_B.ToStringPercent()), 114512);
		}
		if (Props.IfCanScatter)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatEnergyWeaponDMG3Radius_Label".Translate(), "StatEnergyWeaponDMG3Radius_Desc".Translate(Props.ScatterRadius), "StatEnergyWeaponDMG3Radius_Text".Translate(Props.ScatterRadius), 114511);
		}
	}
}
