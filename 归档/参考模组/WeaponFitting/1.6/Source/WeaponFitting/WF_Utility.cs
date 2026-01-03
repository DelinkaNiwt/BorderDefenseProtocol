using System;
using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public static class WF_Utility
{
	public static Dictionary<ThingDef, List<UniqueWeaponCategoriesDef>> UniqueWeaponCategoriesDefByThingDef()
	{
		Dictionary<ThingDef, List<UniqueWeaponCategoriesDef>> dictionary = new Dictionary<ThingDef, List<UniqueWeaponCategoriesDef>>();
		foreach (UniqueWeaponCategoriesDef def in DefDatabase<UniqueWeaponCategoriesDef>.AllDefs.ToList())
		{
			if (def.weaponDefs.NullOrEmpty() || def.weaponCategories.NullOrEmpty())
			{
				continue;
			}
			foreach (ThingDef weapon in def.weaponDefs)
			{
				if (!dictionary.ContainsKey(weapon))
				{
					dictionary[weapon] = new List<UniqueWeaponCategoriesDef>();
				}
				dictionary[weapon].Add(def);
			}
		}
		return dictionary;
	}

	public static List<WeaponCategoryDef> SetWeaponCategories(List<UniqueWeaponCategoriesDef> uniqueWeaponCategories, out int? maxtraits)
	{
		maxtraits = null;
		List<WeaponCategoryDef> weaponCategories = new List<WeaponCategoryDef>();
		foreach (UniqueWeaponCategoriesDef def in uniqueWeaponCategories)
		{
			SetWeaponCategories(weaponCategories, def, out var thismaxtraits);
			if (thismaxtraits.HasValue)
			{
				if (!maxtraits.HasValue)
				{
					maxtraits = thismaxtraits;
				}
				else
				{
					maxtraits = Math.Max(maxtraits.Value, thismaxtraits.Value);
				}
			}
		}
		return weaponCategories;
	}

	public static List<WeaponCategoryDef> SetWeaponCategories(List<WeaponCategoryDef> baseDefs, UniqueWeaponCategoriesDef uniqueWeaponCategory, out int? maxtraits)
	{
		maxtraits = uniqueWeaponCategory.maxTraits;
		if (uniqueWeaponCategory.weaponCategories.NullOrEmpty())
		{
			return baseDefs;
		}
		foreach (WeaponCategoryDef def in uniqueWeaponCategory.weaponCategories)
		{
			if (!baseDefs.Contains(def))
			{
				baseDefs.Add(def);
			}
		}
		return baseDefs;
	}

	public static Dictionary<WeaponCategoryDef, WeaponFittingDef> WeaponFittingDefByWeaponCategoryDef(out List<ThingCategoryDef> categoryDefs)
	{
		Dictionary<WeaponCategoryDef, WeaponFittingDef> dictionary = new Dictionary<WeaponCategoryDef, WeaponFittingDef>();
		categoryDefs = new List<ThingCategoryDef>();
		foreach (WeaponFittingDef def in DefDatabase<WeaponFittingDef>.AllDefs.ToList())
		{
			if (def.weaponCategoryDef != null)
			{
				dictionary[def.weaponCategoryDef] = def;
			}
			if (!def.weaponCategoryDefs.NullOrEmpty())
			{
				foreach (WeaponCategoryDef categoryDef in def.weaponCategoryDefs)
				{
					dictionary[categoryDef] = def;
				}
			}
			if (def.thingCategoryDef != null && !categoryDefs.Contains(def.thingCategoryDef))
			{
				categoryDefs.Add(def.thingCategoryDef);
			}
		}
		return dictionary;
	}

	public static Dictionary<ushort, ThingDef> ThingDefsByShortHash()
	{
		Dictionary<ushort, ThingDef> thingDefsByShortHash = new Dictionary<ushort, ThingDef>();
		foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (!thingDefsByShortHash.TryGetValue(thingDef.shortHash, out var _))
			{
				thingDefsByShortHash.Add(thingDef.shortHash, thingDef);
			}
		}
		return thingDefsByShortHash;
	}

	public static void GiveShortHash(ThingDef def, Dictionary<ushort, ThingDef> thingDefsByShortHash)
	{
		if (def.shortHash != 0)
		{
			Log.Error(def?.ToString() + " already has short hash.");
			return;
		}
		ushort num = (ushort)(GenText.StableStringHash(def.defName) % 65535);
		int num2 = 0;
		while (num == 0 || thingDefsByShortHash.ContainsKey(num))
		{
			num++;
			num2++;
			if (num2 > 5000)
			{
				Log.Message("Short hashes are saturated. There are probably too many Defs.");
			}
		}
		def.shortHash = num;
		thingDefsByShortHash.Add(num, def);
	}

	public static bool HasUniqueComp(ThingDef weapon)
	{
		if (weapon.comps.NullOrEmpty())
		{
			return false;
		}
		foreach (CompProperties comp in weapon.comps)
		{
			if (comp is CompProperties_UniqueWeapon || comp is CompProperties_EmptyUniqueWeapon)
			{
				return true;
			}
		}
		return false;
	}

	public static void DrawRenameButton(Rect rect, Thing weapon)
	{
		CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
		if (comp != null)
		{
			TooltipHandler.TipRegionByKey(rect, "Ancot.RenameWeapon");
			if (Widgets.ButtonImage(rect, TexButton.Rename))
			{
				Find.WindowStack.Add(new Dialog_NameWeapon(weapon));
			}
		}
	}
}
