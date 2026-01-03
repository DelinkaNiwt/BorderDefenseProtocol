using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompRangeWeaponVerbSwitch_EnergyPassive : ThingComp
{
	public bool switched = false;

	public bool lastSwitched = false;

	private CompProperties_RangeWeaponVerbSwitch_EnergyPassive Props => (CompProperties_RangeWeaponVerbSwitch_EnergyPassive)props;

	public CompWeaponCharge compWeaponCharge => parent.TryGetComp<CompWeaponCharge>();

	public CompEquippable compEquippable => parent.TryGetComp<CompEquippable>();

	public CompSustainedShoot CompSustainedShoot => parent.TryGetComp<CompSustainedShoot>();

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		CompWeaponCharge obj = compWeaponCharge;
		if (obj != null && obj.CanBeUsed)
		{
			Rand.PushState();
			switched = true;
			lastSwitched = switched;
			compEquippable.PrimaryVerb.verbProps = Props.verbProps;
			VerbRefresh();
			Rand.PopState();
		}
	}

	public void Notify_SwitchPassive()
	{
		if (compWeaponCharge != null)
		{
			switched = compWeaponCharge.CanBeUsed;
			compEquippable.PrimaryVerb.verbProps = (switched ? Props.verbProps : parent.def.Verbs[0]);
			if (switched != lastSwitched)
			{
				VerbRefresh();
			}
			lastSwitched = switched;
		}
	}

	public void VerbRefresh()
	{
		EquipmentUtility.VerbRefresh(compEquippable);
		CompSustainedShoot?.VerbReset();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref switched, "switched", defaultValue: false);
		Scribe_Values.Look(ref lastSwitched, "lastSwithced", defaultValue: false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && switched)
		{
			compEquippable.PrimaryVerb.verbProps = Props.verbProps;
		}
	}

	public string VerbSwitchInfo()
	{
		return string.Concat(string.Concat(string.Concat("Ancot.VerbSwitchInfoDesc".Translate() + "\n\n" + "Ancot.VerbSwitch_BurstShotCount".Translate() + ": ", Props.verbProps.burstShotCount.ToString(), "\n") + "Ancot.VerbSwitch_Range".Translate() + ": ", Mathf.RoundToInt(Props.verbProps.range).ToString(), "\n") + "Ancot.VerbSwitch_WarmupTime".Translate() + ": ", Props.verbProps.warmupTime.ToString(), " ") + "Ancot.Second".Translate();
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Ancot.SwitchVerb".Translate(), Props.verbProps.label, VerbSwitchInfo(), 5600);
	}
}
