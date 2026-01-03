using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class WeaponTraitsUtility
{
	public static AcceptanceReport CanUseTraits(ThingWithComps weapon, out CompUniqueWeapon comp)
	{
		comp = null;
		if (weapon == null)
		{
			return "Anoct.WeaponIsNull".Translate();
		}
		if (weapon.AllComps.NullOrEmpty())
		{
			return "Ancot.WeaponCantUseTrait".Translate(weapon.def.label);
		}
		comp = weapon.TryGetComp<CompUniqueWeapon>();
		if (comp == null)
		{
			return "Ancot.WeaponCantUseTrait".Translate(weapon.def.label);
		}
		return true;
	}

	public static AcceptanceReport CanAddTraits(WeaponTraitDef trait, ThingWithComps weapon, out List<WeaponTraitDef> replacedTraits)
	{
		replacedTraits = new List<WeaponTraitDef>();
		if (trait == null)
		{
			Log.Error("Anoct.TraitIsNull".Translate());
			return false;
		}
		CompUniqueWeapon comp;
		AcceptanceReport acceptanceReport = CanUseTraits(weapon, out comp);
		if (!acceptanceReport)
		{
			return acceptanceReport;
		}
		if (!comp.Props.weaponCategories.Contains(trait.weaponCategory))
		{
			return false;
		}
		List<WeaponTraitDef> traitsListForReading = comp.TraitsListForReading;
		if (traitsListForReading.Empty())
		{
			return (!trait.canGenerateAlone) ? ((AcceptanceReport)"Anoct.CantAddTraitAlone".Translate(trait.label)) : ((AcceptanceReport)true);
		}
		if (!traitsListForReading.NullOrEmpty())
		{
			bool flag = false;
			foreach (WeaponTraitDef item in traitsListForReading)
			{
				if (trait.Overlaps(item))
				{
					replacedTraits.Add(item);
					flag = true;
				}
			}
			if (flag)
			{
				return true;
			}
		}
		if (traitsListForReading.Count >= 3)
		{
			return "Anoct.TooManyTraits".Translate(weapon.LabelShortCap);
		}
		if (comp is CompEmptyUniqueWeapon compEmptyUniqueWeapon)
		{
			int max_traits = compEmptyUniqueWeapon.Traits_Props.max_traits;
			if (traitsListForReading.Count >= max_traits)
			{
				return "Anoct.TooManyTraits".Translate(weapon.LabelShortCap);
			}
		}
		return true;
	}

	public static void AddOrReplaceTrait(WeaponTraitDef trait, ThingWithComps weapon, out List<WeaponTraitDef> replacedTraits, Pawn pawn = null)
	{
		if (!CanAddTraits(trait, weapon, out replacedTraits))
		{
			return;
		}
		CompEquippable compEquippable = weapon?.TryGetComp<CompEquippable>();
		if (compEquippable == null)
		{
			Log.Error("Not Exist a Weapon");
			return;
		}
		EquipmentUtility.VerbRefresh(compEquippable);
		CompUniqueWeapon compUniqueWeapon = weapon.TryGetComp<CompUniqueWeapon>();
		if (replacedTraits.NullOrEmpty())
		{
			compUniqueWeapon.AddTrait(trait);
		}
		else
		{
			ReplaceTrait(trait, replacedTraits, compUniqueWeapon);
		}
		EquipmentUtility.VerbRefresh(compEquippable);
		compUniqueWeapon.Setup(fromSave: false);
		ClearWeaponCache(weapon);
		if (pawn != null)
		{
			CompEquippableAbility compEquippableAbility = weapon.TryGetComp<CompEquippableAbility>();
			CompEquippableAbilityReloadable compEquippableAbilityReloadable = weapon.TryGetComp<CompEquippableAbilityReloadable>();
			compEquippableAbility?.Notify_Equipped(pawn);
			if (compEquippableAbilityReloadable != null)
			{
				compEquippableAbilityReloadable.RemainingCharges = 0;
			}
			pawn.abilities.Notify_TemporaryAbilitiesChanged();
		}
		weapon.AddToGC();
	}

	public static void ReplaceTrait(WeaponTraitDef trait, List<WeaponTraitDef> replacedTreats, CompUniqueWeapon comp)
	{
		RemoveTraits(replacedTreats, comp);
		comp.AddTrait(trait);
	}

	public static void RemoveTraits(List<WeaponTraitDef> traits, CompUniqueWeapon comp)
	{
		if (traits.NullOrEmpty())
		{
			Log.Error("No traits need to remove");
			return;
		}
		if (traits.Count == 1)
		{
			RemoveTrait(traits[0], comp);
			return;
		}
		List<WeaponTraitDef> traitsListForReading = comp.TraitsListForReading;
		foreach (WeaponTraitDef trait in traits)
		{
			if (traitsListForReading.Contains(trait))
			{
				traitsListForReading.Remove(trait);
			}
			else
			{
				Log.Error("Anoct.TraitNotExist".Translate(trait.label));
			}
		}
		typeof(CompUniqueWeapon).GetField("traits", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(comp, traitsListForReading);
	}

	public static void RemoveAllTraits(CompUniqueWeapon comp)
	{
		List<WeaponTraitDef> traitsListForReading = comp.TraitsListForReading;
		foreach (WeaponTraitDef item in traitsListForReading.ToList())
		{
			RemoveTrait(item, comp);
		}
	}

	public static void RemoveAllTraitsAndDropFittings(Thing weapon, Pawn pawn = null)
	{
		CompUniqueWeapon compUniqueWeapon = weapon.TryGetComp<CompUniqueWeapon>();
		List<WeaponTraitDef> traitsListForReading = compUniqueWeapon.TraitsListForReading;
		foreach (WeaponTraitDef item in traitsListForReading.ToList())
		{
			RemoveTraitAndDropFitting(item, compUniqueWeapon);
		}
		if (pawn != null)
		{
			weapon.TryGetComp<CompEquippableAbility>()?.Notify_Equipped(pawn);
			pawn.abilities.Notify_TemporaryAbilitiesChanged();
		}
		ClearWeaponCache(weapon);
	}

	public static void RemoveTraitAndDropFitting(WeaponTraitDef trait, CompUniqueWeapon comp)
	{
		DropWeaponFitting(trait, comp.parent.PositionHeld, comp.parent.MapHeld);
		RemoveTrait(trait, comp);
	}

	public static void RemoveTrait(WeaponTraitDef trait, CompUniqueWeapon comp)
	{
		ThingWithComps parent = comp.parent;
		CompEquippable compEquippable = parent?.TryGetComp<CompEquippable>();
		if (parent == null || compEquippable == null)
		{
			Log.Error("Not Exist a Weapon");
			return;
		}
		List<WeaponTraitDef> traitsListForReading = comp.TraitsListForReading;
		if (traitsListForReading.Contains(trait))
		{
			traitsListForReading.Remove(trait);
			EquipmentUtility.VerbRefresh(compEquippable);
			if (trait.abilityProps != null)
			{
				CompEquippableAbilityReloadable compEquippableAbilityReloadable = parent.TryGetComp<CompEquippableAbilityReloadable>();
				compEquippableAbilityReloadable.props = new CompProperties_EquippableAbilityReloadable();
				EquipmentUtility.CompRefresh(compEquippable);
				compEquippableAbilityReloadable.Notify_PropsChanged();
			}
		}
		else
		{
			Log.Error("Anoct.TraitNotExist".Translate(trait.label));
		}
		typeof(CompUniqueWeapon).GetField("traits", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(comp, traitsListForReading);
		StatDefOf.RangedWeapon_RangeMultiplier.Worker.ClearCacheForThing(parent);
		Log.Message("field" + string.Join(",", traitsListForReading));
		if (comp.IsTraitsEmpty())
		{
			comp.parent.RemoveFromGC();
		}
	}

	public static void DropWeaponFitting(WeaponTraitDef trait, IntVec3 pos, Map map, int count = 1)
	{
		ThingDef thingDef = FittingDef(trait);
		if (thingDef != null)
		{
			Thing thing = ThingMaker.MakeThing(thingDef);
			thing.stackCount = count;
			GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
		}
	}

	public static ThingDef FittingDef(WeaponTraitDef trait)
	{
		string defName = "Ancot_WeaponFitting_" + trait.defName;
		return DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false);
	}

	public static bool IsWeaponFitting(Thing thing, out WeaponTraitDef trait)
	{
		trait = null;
		CompWeaponFitting compWeaponFitting = thing.TryGetComp<CompWeaponFitting>();
		if (compWeaponFitting == null)
		{
			return false;
		}
		trait = compWeaponFitting.Props.trait;
		if (trait == null)
		{
			return false;
		}
		return true;
	}

	public static void ClearWeaponCache(Thing weapon)
	{
		CompUniqueWeapon compUniqueWeapon = weapon.TryGetComp<CompUniqueWeapon>();
		foreach (WeaponTraitDef item in compUniqueWeapon.TraitsListForReading)
		{
			if (item.statFactors != null)
			{
				foreach (StatModifier statFactor in item.statFactors)
				{
					statFactor.stat.Worker.ClearCacheForThing(weapon);
				}
			}
			if (item.statOffsets == null)
			{
				continue;
			}
			foreach (StatModifier statOffset in item.statOffsets)
			{
				statOffset.stat.Worker.ClearCacheForThing(weapon);
			}
		}
	}

	public static StringBuilder TraitSrting(WeaponTraitDef weaponTraitDef)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(weaponTraitDef.LabelCap.Colorize(ColorLibrary.Yellow));
		stringBuilder.AppendLine(weaponTraitDef.description);
		if (!weaponTraitDef.statOffsets.NullOrEmpty())
		{
			stringBuilder.Append(weaponTraitDef.statOffsets.Select((StatModifier x) => $"{x.stat.LabelCap} {x.stat.Worker.ValueToString(x.value, finalized: false, ToStringNumberSense.Offset)}").ToLineList(" - "));
			stringBuilder.AppendLine();
		}
		if (!weaponTraitDef.statFactors.NullOrEmpty())
		{
			stringBuilder.Append(weaponTraitDef.statFactors.Select((StatModifier x) => $"{x.stat.LabelCap} {x.stat.Worker.ValueToString(x.value, finalized: false, ToStringNumberSense.Factor)}").ToLineList(" - "));
			stringBuilder.AppendLine();
		}
		if (!Mathf.Approximately(weaponTraitDef.burstShotCountMultiplier, 1f))
		{
			stringBuilder.AppendLine(string.Format(" - {0} {1}", "StatsReport_BurstShotCountMultiplier".Translate(), weaponTraitDef.burstShotCountMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)));
		}
		if (!Mathf.Approximately(weaponTraitDef.burstShotSpeedMultiplier, 1f))
		{
			stringBuilder.AppendLine(string.Format(" - {0} {1}", "StatsReport_BurstShotSpeedMultiplier".Translate(), weaponTraitDef.burstShotSpeedMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)));
		}
		if (!Mathf.Approximately(weaponTraitDef.additionalStoppingPower, 0f))
		{
			stringBuilder.AppendLine(string.Format(" - {0} {1}", "StatsReport_AdditionalStoppingPower".Translate(), weaponTraitDef.additionalStoppingPower.ToStringByStyle(ToStringStyle.FloatOne, ToStringNumberSense.Offset)));
		}
		return stringBuilder;
	}

	public static WeaponTraitDef RandomTraitExceptBladeLink()
	{
		IEnumerable<WeaponTraitDef> enumerable = from trait in DefDatabase<WeaponTraitDef>.AllDefs.ToList()
			where trait != null && trait.weaponCategory != WeaponCategoryDefOf.BladeLink
			select trait;
		return enumerable.EnumerableNullOrEmpty() ? null : enumerable.RandomElement();
	}

	public static WeaponTraitDef RandomTrait(List<WeaponCategoryDef> IsweaponCategoryDefs = null, List<WeaponCategoryDef> NotweaponCategoryDefs = null)
	{
		bool containNeed = !IsweaponCategoryDefs.NullOrEmpty();
		bool notContainNeed = !NotweaponCategoryDefs.NullOrEmpty();
		IEnumerable<WeaponTraitDef> enumerable = from trait in DefDatabase<WeaponTraitDef>.AllDefs.ToList()
			where trait != null && (!containNeed || trait.IsWeaponCategoryDefsContain(IsweaponCategoryDefs)) && (!notContainNeed || trait.IsWeaponCategoryDefsNotContain(NotweaponCategoryDefs))
			select trait;
		return enumerable.EnumerableNullOrEmpty() ? null : enumerable.RandomElement();
	}

	public static bool IsWeaponCategoryDefsContain(this WeaponTraitDef traitDef, List<WeaponCategoryDef> weaponCategoryDefs)
	{
		foreach (WeaponCategoryDef weaponCategoryDef in weaponCategoryDefs)
		{
			if (traitDef.IsWeaponCategoryDefContain(weaponCategoryDef))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsWeaponCategoryDefsNotContain(this WeaponTraitDef traitDef, List<WeaponCategoryDef> weaponCategoryDefs)
	{
		foreach (WeaponCategoryDef weaponCategoryDef in weaponCategoryDefs)
		{
			if (!traitDef.IsWeaponCategoryDefNotContain(weaponCategoryDef))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsWeaponCategoryDefContain(this WeaponTraitDef traitDef, WeaponCategoryDef categoryDef)
	{
		return traitDef.weaponCategory == categoryDef;
	}

	public static bool IsWeaponCategoryDefNotContain(this WeaponTraitDef traitDef, WeaponCategoryDef categoryDef)
	{
		return traitDef.weaponCategory != categoryDef;
	}

	public static bool IsTraitsEmpty(this CompUniqueWeapon comp)
	{
		if (comp.TraitsListForReading.Empty())
		{
			return true;
		}
		return false;
	}

	public static void RemoveFromGC(this Thing weapon)
	{
		if (weapon == null || weapon.Destroyed)
		{
			Log.Error("Need Weapon");
			return;
		}
		GameComponent_AncotLibrary gC = GameComponent_AncotLibrary.GC;
		if (gC == null)
		{
			Log.Error("Need GameComponent_AncotLibrary");
		}
		else if (gC != null && gC.SpecialWeapon.Contains(weapon))
		{
			gC.SpecialWeapon.Remove(weapon);
		}
	}

	public static void AddToGC(this Thing weapon)
	{
		if (weapon == null || weapon.Destroyed)
		{
			Log.Error("Need Weapon");
			return;
		}
		GameComponent_AncotLibrary gC = GameComponent_AncotLibrary.GC;
		if (gC == null)
		{
			Log.Error("Need GameComponent_AncotLibrary");
		}
		else if (gC != null && !gC.SpecialWeapon.Contains(weapon))
		{
			gC.SpecialWeapon.Add(weapon);
		}
	}

	public static int WeaponSlots(this Thing weapon)
	{
		return weapon?.TryGetComp<CompUniqueWeapon>().WeaponSlots() ?? 0;
	}

	public static int WeaponSlots(this CompUniqueWeapon comp)
	{
		if (comp == null)
		{
			return 0;
		}
		return (comp is CompEmptyUniqueWeapon compEmptyUniqueWeapon) ? compEmptyUniqueWeapon.Traits_Props.max_traits : 3;
	}

	public static string UniqueName(this CompUniqueWeapon comp)
	{
		return typeof(CompUniqueWeapon).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(comp) as string;
	}

	public static string UniqueName(this Thing thing)
	{
		CompUniqueWeapon compUniqueWeapon = thing.TryGetComp<CompUniqueWeapon>();
		if (compUniqueWeapon == null)
		{
			Log.Error("Need CompUniqueWeapon");
			return null;
		}
		return compUniqueWeapon.UniqueName();
	}

	public static void SetUniqueName(this Thing thing, string name)
	{
		CompUniqueWeapon compUniqueWeapon = thing.TryGetComp<CompUniqueWeapon>();
		if (compUniqueWeapon == null)
		{
			Log.Error("Need CompUniqueWeapon");
		}
		else
		{
			compUniqueWeapon.SetUniqueName(name);
		}
	}

	public static void SetUniqueName(this CompUniqueWeapon comp, string name)
	{
		typeof(CompUniqueWeapon).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(comp, name);
	}

	public static bool IsWeaponStar(this Thing weapon)
	{
		List<Thing> starWeapon = GameComponent_AncotLibrary.GC.StarWeapon;
		if (starWeapon.Empty())
		{
			return false;
		}
		return starWeapon.Contains(weapon);
	}

	public static Pawn WeaponOwner(this Thing thing)
	{
		if (thing == null || thing.Destroyed)
		{
			return null;
		}
		if (thing.ParentHolder != null)
		{
			Thing thing2 = thing;
			if (thing.ParentHolder.ParentHolder is Pawn { Corpse: var corpse } pawn)
			{
				if (corpse != null)
				{
					thing2 = corpse;
				}
				else
				{
					thing2 = pawn;
				}
				return pawn;
			}
		}
		return null;
	}
}
