using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public static class ThingGenerator_WeaponFittings
{
	public static IEnumerable<ThingDef> ImpliedFittingDefs(bool hotReload = false)
	{
		Dictionary<ushort, ThingDef> thingDefsByShortHash = WF_Utility.ThingDefsByShortHash();
		List<ThingCategoryDef> categoryDefs;
		Dictionary<WeaponCategoryDef, WeaponFittingDef> weaponFittingDefByWeaponCategoryDef = WF_Utility.WeaponFittingDefByWeaponCategoryDef(out categoryDefs);
		WF_weaponPatch(out var SuperlinkItem);
		foreach (WeaponTraitDef traitDef in DefDatabase<WeaponTraitDef>.AllDefs.ToList())
		{
			ThingDef fitting = FittingDef(traitDef, hotReload, ref thingDefsByShortHash);
			if (fitting == null)
			{
				Log.Message(traitDef.defName + "spawnfittingfalse");
				continue;
			}
			List<DefHyperlink> hyperlinks = new List<DefHyperlink>();
			if (traitDef.weaponCategory != null && SuperlinkItem.ContainsKey(traitDef.weaponCategory))
			{
				foreach (ThingDef weapon in SuperlinkItem[traitDef.weaponCategory])
				{
					hyperlinks.Add(weapon);
				}
			}
			fitting.descriptionHyperlinks = hyperlinks;
			bool hascategory = false;
			WeaponFittingDef weaponFittingDef = null;
			if (traitDef.weaponCategory != null && weaponFittingDefByWeaponCategoryDef.ContainsKey(traitDef.weaponCategory))
			{
				weaponFittingDef = weaponFittingDefByWeaponCategoryDef[traitDef.weaponCategory];
			}
			if (weaponFittingDef != null)
			{
				if (!weaponFittingDef.texPath.NullOrEmpty())
				{
					fitting.graphicData.texPath = weaponFittingDef.texPath;
				}
				if (weaponFittingDef.thingCategoryDef != null)
				{
					fitting.thingCategories = new List<ThingCategoryDef> { weaponFittingDef.thingCategoryDef };
					weaponFittingDef.thingCategoryDef.childThingDefs.Add(fitting);
					hascategory = true;
				}
			}
			if (!hascategory)
			{
				if (traitDef.weaponCategory == WeaponCategoryDefOf.BladeLink)
				{
					fitting.thingCategories = new List<ThingCategoryDef> { WF_DefOf.Ancot_WeaponFitting_Bladelink };
					WF_DefOf.Ancot_WeaponFitting_Bladelink.childThingDefs.Add(fitting);
				}
				else
				{
					WF_DefOf.Ancot_WeaponFitting_Others.childThingDefs.Add(fitting);
				}
			}
			yield return fitting;
		}
		foreach (ThingCategoryDef thingCategoryDef in categoryDefs)
		{
			thingCategoryDef.ResolveReferences();
		}
		foreach (ThingCategoryDef thingCategoryDef2 in categoryDefs)
		{
			thingCategoryDef2.ResolveReferences();
		}
		WF_DefOf.Ancot_WeaponFitting_Others.ResolveReferences();
		WF_DefOf.Ancot_WeaponFitting_Bladelink.ResolveReferences();
		AncotDefOf.Ancot_WeaponFitting.ResolveReferences();
		AncotDefOf.Ancot_WeaponsModification.ResolveReferences();
	}

	public static void WF_weaponPatch(out Dictionary<WeaponCategoryDef, List<ThingDef>> SuperlinkItem)
	{
		Dictionary<ThingDef, List<UniqueWeaponCategoriesDef>> dictionary = WF_Utility.UniqueWeaponCategoriesDefByThingDef();
		SuperlinkItem = new Dictionary<WeaponCategoryDef, List<ThingDef>>();
		foreach (ThingDef weapon in dictionary.Keys)
		{
			if (WF_Utility.HasUniqueComp(weapon))
			{
				continue;
			}
			CompProperties_EmptyUniqueWeapon comp = new CompProperties_EmptyUniqueWeapon();
			int? maxtraits;
			List<WeaponCategoryDef> weaponCategoryDefs = WF_Utility.SetWeaponCategories(dictionary[weapon], out maxtraits);
			if (maxtraits.HasValue)
			{
				comp.max_traits = maxtraits.Value;
			}
			comp.weaponCategories = weaponCategoryDefs;
			bool flag = false;
			bool hasAbility = false;
			CompProperties compEP = new CompProperties();
			foreach (CompProperties compP in weapon.comps)
			{
				if (compP is CompProperties_EmptyUniqueWeapon || compP is CompProperties_UniqueWeapon)
				{
					flag = true;
				}
				if (compP.compClass == typeof(CompEquippable))
				{
					compEP = compP;
				}
				if (compP is CompProperties_EquippableAbility)
				{
					hasAbility = true;
				}
			}
			if (!hasAbility)
			{
				weapon.comps.Remove(compEP);
				weapon.comps.Add(new CompProperties_EquippableAbilityReloadable());
			}
			if (flag)
			{
				continue;
			}
			weapon.comps.Add(comp);
			if (!weapon.thingCategories.Contains(AncotDefOf.Ancot_WeaponsModification))
			{
				weapon.thingCategories.Add(AncotDefOf.Ancot_WeaponsModification);
				AncotDefOf.Ancot_WeaponsModification.childThingDefs.Add(weapon);
			}
			foreach (WeaponCategoryDef def in weaponCategoryDefs)
			{
				if (!SuperlinkItem.ContainsKey(def))
				{
					SuperlinkItem[def] = new List<ThingDef>();
				}
				SuperlinkItem[def].Add(weapon);
			}
		}
	}

	public static ThingDef FittingDef(WeaponTraitDef traitDef, bool hotReload, ref Dictionary<ushort, ThingDef> thingDefsByShortHash)
	{
		string defName = "Ancot_WeaponFitting_" + traitDef.defName;
		ThingDef fitting = (hotReload ? (DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false) ?? new ThingDef()) : new ThingDef());
		fitting.defName = defName;
		fitting.label = "Ancot.WeaponFitting".Translate(traitDef.label);
		fitting.drawerType = DrawerType.MapMeshOnly;
		fitting.resourceReadoutPriority = ResourceCountPriority.Middle;
		fitting.category = ThingCategory.Item;
		fitting.thingClass = typeof(ThingWithComps);
		fitting.graphicData = new GraphicData
		{
			graphicClass = typeof(Graphic_Single),
			drawSize = new Vector2(1f, 1f)
		};
		fitting.useHitPoints = true;
		fitting.selectable = true;
		fitting.alwaysHaulable = true;
		fitting.techLevel = TechLevel.Industrial;
		fitting.SetStatBaseValue(StatDefOf.MaxHitPoints, 60f);
		fitting.SetStatBaseValue(StatDefOf.DeteriorationRate, 0f);
		fitting.SetStatBaseValue(StatDefOf.Mass, 0.03f);
		fitting.SetStatBaseValue(StatDefOf.Flammability, 0f);
		fitting.SetStatBaseValue(StatDefOf.Beauty, -2f);
		fitting.SetStatBaseValue(StatDefOf.MarketValue, 300f);
		fitting.altitudeLayer = AltitudeLayer.Item;
		fitting.stackLimit = 10;
		fitting.rotatable = false;
		fitting.useHitPoints = true;
		fitting.tradeability = Tradeability.None;
		fitting.drawGUIOverlay = true;
		fitting.soundDrop = SoundDefOf.Standard_Drop;
		fitting.tradeability = Tradeability.Sellable;
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(fitting, "soundInteract", "Metal_Drop");
		fitting.thingCategories = new List<ThingCategoryDef> { WF_DefOf.Ancot_WeaponFitting_Others };
		CompProperties_WeaponFittings compProperties_WeaponFittings = new CompProperties_WeaponFittings
		{
			trait = traitDef
		};
		fitting.comps.Add(compProperties_WeaponFittings);
		CompProperties_Forbiddable comp = new CompProperties_Forbiddable();
		fitting.comps.Add(comp);
		string desc = "Ancot.WeaponFittingDesc".Translate(traitDef.label) + "\n\n" + WeaponTraitsUtility.TraitSrting(traitDef).ToString();
		fitting.description = desc;
		fitting.graphicData.texPath = "AncotLibrary/Item/UniqueWeapon/WeaponFitting_Base";
		WF_Utility.GiveShortHash(fitting, thingDefsByShortHash);
		return fitting;
	}
}
