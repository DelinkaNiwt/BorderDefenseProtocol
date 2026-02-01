using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CharacterEditor;

internal static class ThingTool
{
	internal enum FreeList
	{
		CategoryFactors,
		CategoryOffsets,
		StatFactors,
		StatOffsets,
		StuffCategories,
		Costs,
		CostsDiff,
		BladeLink,
		Prerequisites,
		ApparelLayer,
		BodyPartGroup,
		All
	}

	internal const string CO_GENBUILD_NEWBLUEPRINTDEF = "NewBlueprintDef_Thing";

	internal const string CO_GENBUILD_NEWFRAMEDEF = "NewFrameDef_Thing";

	internal const string CO_MINIFIED = "Minified";

	internal const string CO_ALLRECIPESCACHED = "allRecipesCached";

	internal static List<StatModifier> lCopyStatFactors = new List<StatModifier>();

	internal static List<StatModifier> lCopyStatOffsets = new List<StatModifier>();

	internal static List<string> lCopyTradeTags = new List<string>();

	internal static List<string> lCopyWeaponTags = new List<string>();

	internal static List<string> lCopyApparelTags = new List<string>();

	internal static List<string> lCopyOutfitTags = new List<string>();

	internal static List<StuffCategoryDef> lCopyStuffCategories = new List<StuffCategoryDef>();

	internal static List<WeaponTraitDef> lCopyBladeLinkTraits = new List<WeaponTraitDef>();

	internal static List<ThingDefCountClass> lCopyCosts = new List<ThingDefCountClass>();

	internal static List<ThingDefCountClass> lCopyCostsDiff = new List<ThingDefCountClass>();

	internal static List<ResearchProjectDef> lCopyPrerequisites = new List<ResearchProjectDef>();

	internal static List<ApparelLayerDef> lCopyApparelLayer = new List<ApparelLayerDef>();

	internal static List<BodyPartGroupDef> lCopyBodyPartGroup = new List<BodyPartGroupDef>();

	private static bool bAllCategories = false;

	private static StatCategoryDef selected_StatFactor_CatDef = null;

	private static StatCategoryDef selected_StatOffset_CatDef = null;

	internal static HashSet<StatCategoryDef> lCategoryDef_Factors;

	internal static HashSet<StatCategoryDef> lCategoryDef_Offsets;

	internal static HashSet<StatDef> lFreeStatDefFactors;

	internal static HashSet<StatDef> lFreeStatDefOffsets;

	internal static HashSet<StuffCategoryDef> lFreeStuffCategories;

	internal static HashSet<ThingDef> lFreeCosts;

	internal static HashSet<ThingDef> lFreeCostsDiff;

	internal static HashSet<ResearchProjectDef> lFreePrerequisites;

	internal static HashSet<ApparelLayerDef> lFreeApparelLayer;

	internal static HashSet<BodyPartGroupDef> lFreeBodyPartGroup;

	internal static HashSet<Tradeability> AllTradeabilities => CEditor.API.ListOf<Tradeability>(EType.Tradeability).ToHashSet();

	internal static HashSet<TechLevel> AllTechLevels => CEditor.API.ListOf<TechLevel>(EType.TechLevel).ToHashSet();

	internal static HashSet<QualityCategory> AllQualityCategory => CEditor.API.Get<HashSet<QualityCategory>>(EType.QualityCategory);

	internal static HashSet<WeaponTraitDef> AllWeaponTraitDef => CEditor.API.Get<HashSet<WeaponTraitDef>>(EType.WeaponTraitDef);

	internal static HashSet<ResearchProjectDef> AllResearchProjectDef => CEditor.API.Get<HashSet<ResearchProjectDef>>(EType.ResearchProjectDef);

	internal static HashSet<GasType?> AllGasTypes => CEditor.API.ListOf<GasType?>(EType.GasType).ToHashSet();

	internal static HashSet<ThingDef> AllBullets => CEditor.API.Get<HashSet<ThingDef>>(EType.Bullet);

	internal static HashSet<ThingCategoryDef> AllThingCategoryDef => CEditor.API.Get<HashSet<ThingCategoryDef>>(EType.ThingCategoryDef);

	internal static HashSet<ThingCategory> AllThingCategory => CEditor.API.Get<HashSet<ThingCategory>>(EType.ThingCategory);

	internal static HashSet<ApparelLayerDef> AllApparelLayerDef => CEditor.API.Get<HashSet<ApparelLayerDef>>(EType.ApparelLayerDef);

	internal static HashSet<BodyPartGroupDef> AllBodyPartGroupDef => CEditor.API.Get<HashSet<BodyPartGroupDef>>(EType.BodyPartGroupDef);

	internal static HashSet<WeaponType> AllWeaponType => CEditor.API.Get<HashSet<WeaponType>>(EType.WeaponType);

	internal static HashSet<SoundDef> AllExplosionSounds => CEditor.API.Get<HashSet<SoundDef>>(EType.ExplosionSound);

	internal static HashSet<EffecterDef> AllEffecterDefs => CEditor.API.Get<HashSet<EffecterDef>>(EType.EffecterDef);

	internal static HashSet<DamageDef> AllDamageDefs => CEditor.API.Get<HashSet<DamageDef>>(EType.DamageDef);

	internal static HashSet<SoundDef> AllGunRelatedSounds => CEditor.API.Get<HashSet<SoundDef>>(EType.GunRelatedSound);

	internal static HashSet<SoundDef> AllGunShotSounds => CEditor.API.Get<HashSet<SoundDef>>(EType.GunShotSound);

	internal static bool UseAllCategories
	{
		get
		{
			return bAllCategories;
		}
		set
		{
			bAllCategories = value;
			SelectedThing.thingDef.UpdateFreeLists(FreeList.All);
		}
	}

	internal static StatCategoryDef StatFactorCategory
	{
		get
		{
			return selected_StatFactor_CatDef;
		}
		set
		{
			selected_StatFactor_CatDef = value;
			SelectedThing.thingDef.UpdateFreeLists(FreeList.CategoryFactors);
			SelectedThing.thingDef.UpdateFreeLists(FreeList.StatFactors);
		}
	}

	internal static StatCategoryDef StatOffsetCategory
	{
		get
		{
			return selected_StatOffset_CatDef;
		}
		set
		{
			selected_StatOffset_CatDef = value;
			SelectedThing.thingDef.UpdateFreeLists(FreeList.CategoryOffsets);
			SelectedThing.thingDef.UpdateFreeLists(FreeList.StatOffsets);
		}
	}

	internal static Selected SelectedThing { get; set; }

	internal static HashSet<SoundDef> GetExplosionSounds()
	{
		HashSet<SoundDef> hashSet = new HashSet<SoundDef>();
		hashSet.Add(null);
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.Verbs.Count > 0 && allDef.Verbs[0].defaultProjectile != null && allDef.Verbs[0].defaultProjectile.projectile.soundExplode != null)
			{
				hashSet.Add(allDef.Verbs[0].defaultProjectile.projectile.soundExplode);
			}
		}
		return hashSet;
	}

	internal static HashSet<SoundDef> GetGunRelatedSounds()
	{
		HashSet<SoundDef> l = new HashSet<SoundDef>();
		l.Add(null);
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			GrabAllWeaponSounds(allDef, ref l);
		}
		return l.OrderBy((SoundDef x) => (x != null) ? x.defName : "").ToHashSet();
	}

	internal static HashSet<SoundDef> GetGunShotSounds()
	{
		HashSet<SoundDef> hashSet = new HashSet<SoundDef>();
		hashSet.Add(null);
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.HasVerb())
			{
				hashSet.Add(allDef.Verbs[0].soundCast);
			}
		}
		return hashSet.OrderBy((SoundDef x) => (x != null) ? x.defName : "").ToHashSet();
	}

	internal static void GrabAllWeaponSounds(ThingDef gun, ref HashSet<SoundDef> l)
	{
		if (gun.Verbs.Count <= 0)
		{
			return;
		}
		if (gun.Verbs[0].defaultProjectile != null)
		{
			if (gun.Verbs[0].defaultProjectile.projectile.soundExplode != null)
			{
				l.Add(gun.Verbs[0].defaultProjectile.projectile.soundExplode);
			}
			if (gun.Verbs[0].defaultProjectile.projectile.soundAmbient != null)
			{
				l.Add(gun.Verbs[0].defaultProjectile.projectile.soundAmbient);
			}
			if (gun.Verbs[0].defaultProjectile.projectile.soundHitThickRoof != null)
			{
				l.Add(gun.Verbs[0].defaultProjectile.projectile.soundHitThickRoof);
			}
			if (gun.Verbs[0].defaultProjectile.projectile.soundImpact != null)
			{
				l.Add(gun.Verbs[0].defaultProjectile.projectile.soundImpact);
			}
			if (gun.Verbs[0].defaultProjectile.projectile.soundImpactAnticipate != null)
			{
				l.Add(gun.Verbs[0].defaultProjectile.projectile.soundImpactAnticipate);
			}
		}
		if (gun.Verbs[0].soundAiming != null)
		{
			l.Add(gun.Verbs[0].soundAiming);
		}
		if (gun.Verbs[0].soundCast != null)
		{
			l.Add(gun.Verbs[0].soundCast);
		}
		if (gun.Verbs[0].soundCastBeam != null)
		{
			l.Add(gun.Verbs[0].soundCastBeam);
		}
		if (gun.Verbs[0].soundCastTail != null)
		{
			l.Add(gun.Verbs[0].soundCastTail);
		}
		if (gun.Verbs[0].soundLanding != null)
		{
			l.Add(gun.Verbs[0].soundLanding);
		}
	}

	internal static bool HasAnyItem(this Pawn pawn)
	{
		return pawn.HasInventoryTracker() && !pawn.inventory.innerContainer.NullOrEmpty();
	}

	internal static Thing RandomInventoryItem(this Pawn pawn)
	{
		return pawn.inventory.innerContainer.RandomElement();
	}

	internal static void Spawn(this Thing t, Rot4 rot4 = default(Rot4), IntVec3 pos = default(IntVec3))
	{
		if (t != null)
		{
			if (pos == default(IntVec3))
			{
				pos = UI.MouseCell();
			}
			if (!pos.InBounds(Find.CurrentMap))
			{
				pos = Find.CurrentMap.AllCells.RandomElement();
			}
			GenSpawn.Spawn(t, pos, Find.CurrentMap, rot4);
		}
	}

	internal static string GetAsSeparatedString(this Thing t)
	{
		if (t == null || t.def.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = text + t.def.defName + "|";
		text = text + t.GetStuffDefName() + "|";
		text = text + t.DrawColor.ColorHexString() + "|";
		text = text + t.GetQuality() + "|";
		text = text + t.stackCount + "|";
		text += t.HitPoints;
		if (t.StyleDef != null)
		{
			text += "|";
			text += t.StyleDef.defName;
		}
		return text;
	}

	internal static string GetAllItemsAsSeparatedString(this Pawn p)
	{
		if (!p.HasInventoryTracker() || p.inventory.innerContainer.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Thing item in p.inventory.innerContainer)
		{
			text += item.GetAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetItemsFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasInventoryTracker())
		{
			return;
		}
		try
		{
			p.inventory.DestroyAll();
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length >= 6)
				{
					string styledefname = ((array3.Length >= 7) ? array3[6] : "");
					Thing thing = GenerateItem(Selected.ByName(array3[0], array3[1], styledefname, array3[2].HexStringToColor(), array3[3].AsInt32(), array3[4].AsInt32()));
					if (thing != null)
					{
						thing.HitPoints = array3[5].AsInt32();
						p.inventory.innerContainer.TryAdd(thing);
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void DestroyAllItems(this Pawn pawn)
	{
		if (pawn.HasInventoryTracker())
		{
			pawn.inventory.innerContainer.ClearAndDestroyContents();
		}
	}

	internal static void DestroyItem(this Pawn pawn, Thing t)
	{
		if (pawn.HasInventoryTracker() && t != null)
		{
			pawn.inventory.innerContainer.Remove(t);
			if (t.def.IsApparel && pawn.HasApparelTracker())
			{
				pawn.outfits.forcedHandler.ForcedApparel.Remove(t as Apparel);
			}
			t.Destroy();
		}
	}

	internal static List<Thing> ListOfCopyItems(this Pawn pawn)
	{
		return pawn.HasAnyItem() ? pawn.inventory.innerContainer.InnerListForReading.ListFullCopy() : null;
	}

	internal static void PasteCopyItems(this Pawn pawn, List<Thing> l)
	{
		if (pawn.HasInventoryTracker())
		{
			pawn.DestroyAllItems();
			if (!l.NullOrEmpty())
			{
				foreach (Thing item in l)
				{
					Thing t = GenerateItem(Selected.ByThing(item));
					pawn.AddItemToInventory(t);
				}
			}
		}
		CEditor.API.UpdateGraphics();
	}

	internal static void AddItemToInventory(this Pawn pawn, Thing t)
	{
		if (pawn.HasInventoryTracker() && t != null)
		{
			pawn.inventory.innerContainer.TryAdd(t);
		}
	}

	internal static void TransferToInventory(this Pawn pawn, Thing t)
	{
		if (pawn.HasInventoryTracker() && t != null)
		{
			if (pawn.HasApparelTracker() && pawn.HasForcedApparel() && t.def.IsApparel)
			{
				pawn.outfits.forcedHandler.ForcedApparel.Remove(t as Apparel);
			}
			if (pawn.inventory.innerContainer == null)
			{
				pawn.inventory.innerContainer = new ThingOwner<Thing>();
			}
			pawn.inventory.innerContainer.TryAddOrTransfer(t);
		}
	}

	internal static void TransferFromInventory(this Pawn pawn, Thing t)
	{
		if (pawn.HasInventoryTracker() && t != null)
		{
			if (pawn.inventory.innerContainer == null)
			{
				pawn.inventory.innerContainer = new ThingOwner<Thing>();
			}
			if (pawn.HasApparelTracker() && t.def.IsApparel)
			{
				pawn.inventory.innerContainer.Remove(t);
				pawn.WearThatApparel(t as Apparel);
			}
			else if (t.def.IsWeapon && pawn.HasEquipmentTracker())
			{
				pawn.inventory.innerContainer.Remove(t);
				pawn.AddWeaponToEquipment(t as ThingWithComps, firstWeapon: true, destroyOld: false);
			}
		}
	}

	internal static void CreateAndAddItem(this Pawn pawn, Selected s)
	{
		if (pawn.HasInventoryTracker())
		{
			Thing t = GenerateItem(s);
			pawn.AddItemToInventory(t);
		}
	}

	internal static Thing GenerateItem(Selected s)
	{
		if (s == null || s.thingDef == null)
		{
			return null;
		}
		ThingDef thingDef = DefTool.ThingDef(s.thingDef.defName);
		if (thingDef == null)
		{
			return null;
		}
		s.stuff = s.thingDef.ThisOrDefaultStuff(s.stuff);
		if (!s.thingDef.MadeFromStuff)
		{
			s.stuff = null;
		}
		Thing thing = ThingMaker.MakeThing(s.thingDef, s.stuff);
		thing.HitPoints = thing.MaxHitPoints;
		thing.SetQuality(s.quality);
		thing.SetDrawColor(s.DrawColor);
		thing.stackCount = s.stackVal;
		if (s.style != null)
		{
			thing.StyleDef = s.style;
			thing.StyleDef.color = s.style.color;
		}
		return thing;
	}

	internal static Thing GenerateRandomItem(ThingCategoryDef tc, bool originalColors = true, bool randomStack = true)
	{
		HashSet<ThingDef> l = ListOfItems(null, tc, ThingCategory.None);
		return GenerateItem(Selected.Random(l, originalColors, randomStack));
	}

	internal static bool IsChunk(this ThingDef td)
	{
		return td != null && !td.thingCategories.NullOrEmpty() && (td.thingCategories.Contains(ThingCategoryDefOf.StoneChunks) || td.thingCategories.Contains(ThingCategoryDefOf.Chunks));
	}

	internal static bool IsMineableMineral(this ThingDef td)
	{
		return td != null && td.defName != null && td.building != null && td.building.mineableThing != null && td.building.isResourceRock;
	}

	internal static bool IsMineableRock(this ThingDef td)
	{
		return td != null && td.defName != null && td.building != null && td.building.mineableThing != null && !td.building.isResourceRock;
	}

	internal static ThingDef GetBaseThingDefFromMinified(this ThingDef miniThingDef)
	{
		ThingDef result = null;
		if (miniThingDef != null && miniThingDef.defName != null && miniThingDef.defName.Contains("Minified"))
		{
			string defName = miniThingDef.defName.Replace("Minified", "");
			result = DefTool.ThingDef(defName);
		}
		return result;
	}

	internal static bool IsMinified(this ThingDef t)
	{
		return t != null && t.defName != null && t.defName.Contains("Minified");
	}

	internal static List<StatCategoryDef> GetAllStatCategoriesApparel()
	{
		List<StatCategoryDef> lequip = GetAllStatCategoriesOnEquip();
		List<StatCategoryDef> list = (from td in DefDatabase<StatCategoryDef>.AllDefs
			where td != StatCategoryDefOf.Building && !lequip.Contains(td)
			orderby td.label
			select td).ToList();
		list.Remove(StatCategoryDefOf.Weapon);
		list.RemoveCategoriesWithoutStats();
		list.Insert(0, null);
		return list;
	}

	internal static List<StatCategoryDef> GetAllStatCategoriesOnEquip()
	{
		List<StatCategoryDef> list = new List<StatCategoryDef>();
		list = new List<StatCategoryDef>();
		list.Add(StatCategoryDefOf.BasicsPawn);
		list.Add(StatCategoryDefOf.EquippedStatOffsets);
		list.Add(StatCategoryDefOf.PawnCombat);
		list.Add(StatCategoryDefOf.PawnMisc);
		list.Add(StatCategoryDefOf.PawnSocial);
		list.Add(StatCategoryDefOf.PawnWork);
		list.Add(StatCategoryDefOf.StuffStatFactors);
		list.RemoveCategoriesWithoutStats();
		list.Insert(0, null);
		return list;
	}

	internal static List<StatCategoryDef> GetAllStatCategoriesWeapon()
	{
		List<StatCategoryDef> lequip = GetAllStatCategoriesOnEquip();
		List<StatCategoryDef> list = (from td in DefDatabase<StatCategoryDef>.AllDefs
			where td != StatCategoryDefOf.Building && !lequip.Contains(td)
			orderby td.label
			select td).ToList();
		list.Remove(StatCategoryDefOf.Apparel);
		list.RemoveCategoriesWithoutStats();
		list.Insert(0, null);
		return list;
	}

	internal static bool HasStatsForCategory(StatCategoryDef s)
	{
		foreach (StatDef allDef in DefDatabase<StatDef>.AllDefs)
		{
			if (allDef.category == s)
			{
				return true;
			}
		}
		return false;
	}

	internal static void RemoveCategoriesWithoutStats(this List<StatCategoryDef> l)
	{
		if (l == null)
		{
			return;
		}
		for (int i = 0; i < l.Count; i++)
		{
			if (!HasStatsForCategory(l[i]))
			{
				l.RemoveAt(i);
				i--;
			}
		}
	}

	internal static void RemoveCategoriesWithoutThings(this List<ThingCategoryDef> l)
	{
		if (l == null)
		{
			return;
		}
		Dictionary<ThingCategoryDef, int> dictionary = new Dictionary<ThingCategoryDef, int>();
		foreach (ThingCategoryDef item in l)
		{
			dictionary.Add(item, 0);
		}
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.thingCategories.NullOrEmpty())
			{
				continue;
			}
			foreach (ThingCategoryDef thingCategory in allDef.thingCategories)
			{
				if (dictionary.ContainsKey(thingCategory))
				{
					dictionary[thingCategory]++;
				}
			}
		}
		foreach (ThingCategoryDef key in dictionary.Keys)
		{
			if (dictionary[key] == 0)
			{
				l.Remove(key);
			}
		}
	}

	internal static void AddEquipStat(this ThingDef t, StatDef stat, float value)
	{
		if (t == null || stat == null)
		{
			return;
		}
		if (t.HasEquipStat(stat))
		{
			t.UpdateEquipStat(stat, value);
		}
		else
		{
			StatModifier statModifier = new StatModifier();
			statModifier.stat = stat;
			statModifier.value = value;
			if (t.equippedStatOffsets == null)
			{
				t.equippedStatOffsets = new List<StatModifier>();
			}
			t.equippedStatOffsets.Add(statModifier);
		}
		t.ResolveReferences();
	}

	internal static void AddStat(this ThingDef t, StatDef stat, float value)
	{
		if (t == null || stat == null)
		{
			return;
		}
		if (t.HasStat(stat))
		{
			t.UpdateStat(stat, value);
		}
		else
		{
			StatModifier statModifier = new StatModifier();
			statModifier.stat = stat;
			statModifier.value = value;
			if (t.statBases == null)
			{
				t.statBases = new List<StatModifier>();
			}
			t.statBases.Add(statModifier);
		}
		t.ResolveReferences();
	}

	internal static List<StatDef> GetAllApparelStatDefs()
	{
		List<StatCategoryDef> lequip = GetAllStatCategoriesOnEquip();
		return (from td in DefDatabase<StatDef>.AllDefs
			where !lequip.Contains(td.category) || td.category == StatCategoryDefOf.Apparel
			orderby td.label
			select td).ToList();
	}

	internal static List<StatDef> GetAllOnEquipStatDefs()
	{
		List<StatCategoryDef> lequip = GetAllStatCategoriesOnEquip();
		return (from td in DefDatabase<StatDef>.AllDefs
			where lequip.Contains(td.category)
			orderby td.label
			select td).ToList();
	}

	internal static List<StatDef> GetAllWeaponStatDefs()
	{
		List<StatCategoryDef> lequip = GetAllStatCategoriesOnEquip();
		return (from td in DefDatabase<StatDef>.AllDefs
			where !lequip.Contains(td.category) || td.category == StatCategoryDefOf.Weapon
			orderby td.label
			select td).ToList();
	}

	internal static float GetEquipStatValue(this ThingDef t, StatDef stat)
	{
		if (t != null && t.equippedStatOffsets != null)
		{
			foreach (StatModifier equippedStatOffset in t.equippedStatOffsets)
			{
				if (equippedStatOffset.stat == stat)
				{
					return equippedStatOffset.value;
				}
			}
		}
		return 0f;
	}

	internal static float GetStatValue(this ThingDef t, StatDef stat)
	{
		if (t != null && t.statBases != null)
		{
			foreach (StatModifier statBasis in t.statBases)
			{
				if (statBasis.stat == stat)
				{
					return statBasis.value;
				}
			}
		}
		return 0f;
	}

	internal static bool HasEquipStat(this ThingDef t, StatDef stat)
	{
		if (t != null && t.equippedStatOffsets != null)
		{
			foreach (StatModifier equippedStatOffset in t.equippedStatOffsets)
			{
				if (equippedStatOffset.stat == stat)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool HasStat(this ThingDef t, StatDef stat)
	{
		if (t != null && t.statBases != null)
		{
			foreach (StatModifier statBasis in t.statBases)
			{
				if (statBasis.stat == stat)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void PasteEquipStats(this ThingDef t, List<StatModifier> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.equippedStatOffsets == null)
		{
			t.equippedStatOffsets = new List<StatModifier>();
		}
		foreach (StatModifier item in l)
		{
			t.AddEquipStat(item.stat, item.value);
		}
		t.ResolveReferences();
	}

	internal static void PasteStats(this ThingDef t, List<StatModifier> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.statBases == null)
		{
			t.statBases = new List<StatModifier>();
		}
		foreach (StatModifier item in l)
		{
			t.AddStat(item.stat, item.value);
		}
		t.ResolveReferences();
	}

	internal static void RemoveEquipStat(this ThingDef t, StatDef stat)
	{
		if (t == null || t.equippedStatOffsets == null)
		{
			return;
		}
		foreach (StatModifier equippedStatOffset in t.equippedStatOffsets)
		{
			if (equippedStatOffset.stat == stat)
			{
				t.equippedStatOffsets.Remove(equippedStatOffset);
				break;
			}
		}
		if (t.equippedStatOffsets.Count == 0)
		{
			t.equippedStatOffsets = null;
		}
		t.ResolveReferences();
	}

	internal static void RemoveStat(this ThingDef t, StatDef stat)
	{
		if (t == null || t.statBases == null)
		{
			return;
		}
		foreach (StatModifier statBasis in t.statBases)
		{
			if (statBasis.stat == stat)
			{
				t.statBases.Remove(statBasis);
				break;
			}
		}
		if (t.statBases.Count == 0)
		{
			t.statBases = null;
		}
		t.ResolveReferences();
	}

	internal static void UpdateEquipStat(this ThingDef t, StatDef stat, float value)
	{
		if (t == null || t.equippedStatOffsets == null)
		{
			return;
		}
		if (t.HasEquipStat(stat))
		{
			foreach (StatModifier equippedStatOffset in t.equippedStatOffsets)
			{
				if (equippedStatOffset.stat == stat)
				{
					equippedStatOffset.value = value;
					break;
				}
			}
		}
		else
		{
			t.AddEquipStat(stat, value);
		}
		t.ResolveReferences();
	}

	internal static void UpdateStat(this ThingDef t, StatDef stat, float value)
	{
		if (t == null || t.statBases == null)
		{
			return;
		}
		if (t.HasStat(stat))
		{
			foreach (StatModifier statBasis in t.statBases)
			{
				if (statBasis.stat == stat)
				{
					statBasis.value = value;
					break;
				}
			}
		}
		else
		{
			t.AddStat(stat, value);
		}
		t.ResolveReferences();
	}

	internal static ThingDef RandomAllowedStuff(this ThingDef t)
	{
		return (t != null && t.MadeFromStuff && t.stuffCategories != null) ? GenStuff.AllowedStuffsFor(t).ToList().RandomElement() : null;
	}

	internal static ThingDef ThisOrDefaultStuff(this ThingDef t, ThingDef stuff)
	{
		return (t != null && t.MadeFromStuff && stuff == null) ? GenStuff.DefaultStuffFor(t) : stuff;
	}

	internal static ThingStyleDef RandomAllowedStyle(this ThingDef thingDef)
	{
		return ListOfThingStyleDefs(thingDef, null, withNull: true).RandomElement();
	}

	internal static string GetStuffDefName(this Thing t)
	{
		return (t.Stuff != null) ? t.Stuff.defName : "";
	}

	internal static void AddStuff(this ThingDef t, StuffCategoryDef stuffcat)
	{
		if (t != null && stuffcat != null)
		{
			if (t.stuffCategories == null)
			{
				t.stuffCategories = new List<StuffCategoryDef>();
			}
			if (!t.HasStuff(stuffcat))
			{
				t.stuffCategories.Add(stuffcat);
			}
			t.ResolveReferences();
		}
	}

	internal static List<StuffCategoryDef> GetAllStuffCategories()
	{
		return DefDatabase<StuffCategoryDef>.AllDefs.OrderBy((StuffCategoryDef td) => td.label).ToList();
	}

	internal static bool HasStuff(this ThingDef t, StuffCategoryDef stuffcat)
	{
		if (t != null && t.stuffCategories != null)
		{
			return t.stuffCategories.Contains(stuffcat);
		}
		return false;
	}

	internal static void PasteStuff(this ThingDef t, List<StuffCategoryDef> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.stuffCategories == null)
		{
			t.stuffCategories = new List<StuffCategoryDef>();
		}
		foreach (StuffCategoryDef item in l)
		{
			t.AddStuff(item);
		}
	}

	internal static void RemoveStuff(this ThingDef t, StuffCategoryDef stuffCat)
	{
		if (t == null || t.stuffCategories == null)
		{
			return;
		}
		foreach (StuffCategoryDef stuffCategory in t.stuffCategories)
		{
			if (stuffCategory == stuffCat)
			{
				t.stuffCategories.Remove(stuffCategory);
				break;
			}
		}
		if (t.stuffCategories.Count == 0)
		{
			t.stuffCategories = null;
		}
		t.ResolveReferences();
	}

	internal static ThingDef GetStuff(this ThingDef thingDef, ref HashSet<ThingDef> lOfStuff, ref int stuffIndex, bool random = false)
	{
		lOfStuff = new HashSet<ThingDef>();
		if (thingDef == null || !thingDef.MadeFromStuff)
		{
			return null;
		}
		lOfStuff = (from d in GenStuff.AllowedStuffsFor(thingDef)
			orderby d.stuffProps?.categories.FirstOrDefault().SDefname()
			select d).ThenBy((ThingDef t) => t.label).ToHashSet();
		if (random)
		{
			stuffIndex = CEditor.zufallswert.Next(lOfStuff.Count);
		}
		if (!lOfStuff.NullOrEmpty())
		{
			if (lOfStuff.Count > stuffIndex)
			{
				return lOfStuff.At(stuffIndex);
			}
			stuffIndex = 0;
			return lOfStuff.At(stuffIndex);
		}
		return null;
	}

	internal static bool IsStackable(this ThingDef def)
	{
		return def != null && (def.CountAsResource || def.IsChunk());
	}

	internal static void UpdateStackLimit(this ThingDef def)
	{
		if (def.IsStackable())
		{
			def.deepCountPerCell = def.stackLimit;
			if (!def.CountAsResource)
			{
				def.resourceReadoutAlwaysShow = def.deepCountPerCell > 1;
				def.resourceReadoutPriority = ResourceCountPriority.Middle;
				def.drawGUIOverlay = true;
				def.passability = Traversability.Standable;
				ResourceCounter.ResetDefs();
			}
		}
	}

	internal static void UpdateCostStuffCount(this ThingDef product)
	{
		if (product == null)
		{
			return;
		}
		foreach (RecipeDef allDef in DefDatabase<RecipeDef>.AllDefs)
		{
			if (allDef.ProducedThingDef == product && !allDef.ingredients.NullOrEmpty())
			{
				allDef.ingredients[0].SetBaseCount(product.costStuffCount);
			}
		}
	}

	internal static void UpdateRecipes(this ThingDef product)
	{
		if (product == null)
		{
			return;
		}
		foreach (RecipeDef allDef in DefDatabase<RecipeDef>.AllDefs)
		{
			if (allDef.ProducedThingDef != product)
			{
				continue;
			}
			if (product.recipeMaker != null)
			{
				allDef.researchPrerequisite = product.recipeMaker.researchPrerequisite;
			}
			if (allDef.fixedIngredientFilter != null)
			{
				allDef.fixedIngredientFilter.SetDisallowAll();
			}
			if (!product.stuffCategories.NullOrEmpty())
			{
				if (allDef.ingredients != null)
				{
					allDef.ingredients.Clear();
				}
				allDef.ClearCachedData();
				IngredientCount ingredientCount = new IngredientCount();
				ingredientCount.filter.allowedHitPointsConfigurable = false;
				ingredientCount.filter.allowedQualitiesConfigurable = false;
				List<ThingDef> list = GenStuff.AllowedStuffsFor(product).ToList();
				if (!list.NullOrEmpty())
				{
					if (allDef.fixedIngredientFilter == null)
					{
						allDef.fixedIngredientFilter = new ThingFilter();
					}
					foreach (ThingDef item in list)
					{
						ingredientCount.filter.AllowedThingDefs.AddItem(item);
						ingredientCount.filter.SetAllow(item, allow: true);
						ingredientCount.filter.SetAllowAllWhoCanMake(item);
						ingredientCount.filter.Allows(item);
						allDef.fixedIngredientFilter.AllowedThingDefs.AddItem(item);
						allDef.fixedIngredientFilter.SetAllow(item, allow: true);
						allDef.fixedIngredientFilter.SetAllowAllWhoCanMake(item);
						allDef.fixedIngredientFilter.Allows(item);
					}
				}
				ingredientCount.SetBaseCount(product.costStuffCount);
				if (allDef.ingredients == null)
				{
					allDef.ingredients = new List<IngredientCount>();
				}
				allDef.ingredients.Add(ingredientCount);
				ingredientCount.filter.ResolveReferences();
				ingredientCount.ResolveReferences();
			}
			else if (!product.costList.NullOrEmpty())
			{
				if (allDef.ingredients != null)
				{
					allDef.ingredients.Clear();
				}
				allDef.ClearCachedData();
				if (allDef.ingredients == null)
				{
					allDef.ingredients = new List<IngredientCount>();
				}
				foreach (ThingDefCountClass cost in product.costList)
				{
					IngredientCount ingredientCount2 = new IngredientCount();
					ingredientCount2.filter.AllowedThingDefs.AddItem(cost.thingDef);
					ingredientCount2.filter.SetAllow(cost.thingDef, allow: true);
					ingredientCount2.filter.SetAllowAllWhoCanMake(cost.thingDef);
					ingredientCount2.filter.Allows(cost.thingDef);
					ingredientCount2.SetBaseCount(cost.count);
					allDef.ingredients.Add(ingredientCount2);
					ingredientCount2.filter.ResolveReferences();
					ingredientCount2.ResolveReferences();
				}
			}
			else if (product.costListForDifficulty != null && !product.costListForDifficulty.costList.NullOrEmpty())
			{
				if (allDef.ingredients != null)
				{
					allDef.ingredients.Clear();
				}
				allDef.ClearCachedData();
				if (allDef.ingredients == null)
				{
					allDef.ingredients = new List<IngredientCount>();
				}
				foreach (ThingDefCountClass cost2 in product.costListForDifficulty.costList)
				{
					IngredientCount ingredientCount3 = new IngredientCount();
					ingredientCount3.filter.AllowedThingDefs.AddItem(cost2.thingDef);
					ingredientCount3.filter.SetAllow(cost2.thingDef, allow: true);
					ingredientCount3.filter.SetAllowAllWhoCanMake(cost2.thingDef);
					ingredientCount3.filter.Allows(cost2.thingDef);
					ingredientCount3.SetBaseCount(cost2.count);
					allDef.ingredients.Add(ingredientCount3);
					ingredientCount3.filter.ResolveReferences();
					ingredientCount3.ResolveReferences();
				}
			}
			if (allDef.fixedIngredientFilter != null)
			{
				allDef.fixedIngredientFilter.allowedHitPointsConfigurable = false;
				allDef.fixedIngredientFilter.allowedQualitiesConfigurable = false;
				allDef.fixedIngredientFilter.ResolveReferences();
			}
			allDef.ResolveReferences();
		}
		product.SetMemberValue("allRecipesCached", null);
		if (product.recipeMaker == null || product.recipeMaker.recipeUsers.NullOrEmpty())
		{
			return;
		}
		foreach (ThingDef recipeUser in product.recipeMaker.recipeUsers)
		{
			recipeUser.ResolveReferences();
		}
	}

	[Obsolete("do not use", false)]
	internal static void AddCost(this ThingDef t, ThingDef tcost, int value)
	{
		if (t == null)
		{
			return;
		}
		if (t.HasCost(tcost))
		{
			t.UpdateCost(tcost, value);
		}
		else
		{
			ThingDefCountClass item = new ThingDefCountClass(tcost, value);
			if (t.costList == null)
			{
				t.costList = new List<ThingDefCountClass>();
			}
			t.costList.Add(item);
		}
		t.UpdateRecipes();
		t.ResolveReferences();
	}

	internal static List<ThingDef> GetAllCostThingDefs()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where !td.label.NullOrEmpty() && !td.IsWeapon && !td.IsApparel && td.CountAsResource && !td.mineable && td.plant == null && !td.IsSmoothed && !td.IsDrug && !td.IsIngestible
			orderby td.label
			select td).ToList();
	}

	[Obsolete("do not use", false)]
	internal static bool HasCost(this ThingDef t, ThingDef tcost)
	{
		if (t != null && t.costList != null)
		{
			foreach (ThingDefCountClass cost in t.costList)
			{
				if (cost.thingDef == tcost)
				{
					return true;
				}
			}
		}
		return false;
	}

	[Obsolete("do not use", false)]
	internal static void PasteCost(this ThingDef t, List<ThingDefCountClass> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.costList == null)
		{
			t.costList = new List<ThingDefCountClass>();
		}
		foreach (ThingDefCountClass item in l)
		{
			t.costList.Add(new ThingDefCountClass(item.thingDef, item.count));
		}
	}

	[Obsolete("do not use", false)]
	internal static void RemoveCost(this ThingDef t, ThingDef tcost)
	{
		if (t == null || t.costList == null)
		{
			return;
		}
		foreach (ThingDefCountClass cost in t.costList)
		{
			if (cost.thingDef == tcost)
			{
				t.costList.Remove(cost);
				break;
			}
		}
		if (t.costList.Count == 0)
		{
			t.costList = null;
		}
		t.UpdateRecipes();
		t.ResolveReferences();
	}

	[Obsolete("do not use", false)]
	internal static void UpdateCost(this ThingDef t, ThingDef tcost, int value)
	{
		if (t == null || t.costList == null)
		{
			return;
		}
		if (t.HasCost(tcost))
		{
			foreach (ThingDefCountClass cost in t.costList)
			{
				if (cost.thingDef == tcost)
				{
					cost.count = value;
				}
			}
		}
		else
		{
			t.AddCost(tcost, value);
		}
		t.UpdateRecipes();
		t.ResolveReferences();
	}

	internal static void AddCompColorable(this ThingDef t)
	{
		if (t != null && !t.HasComp(typeof(CompColorable)))
		{
			CompProperties compProperties = new CompProperties();
			compProperties.compClass = typeof(CompColorable);
			if (t.comps == null)
			{
				t.comps = new List<CompProperties>();
			}
			t.comps.Add(compProperties);
			compProperties.ResolveReferences(t);
			t.ResolveReferences();
			t.PostLoad();
		}
	}

	internal static void AddCompExplosive(this ThingDef t)
	{
		if (t == null)
		{
			return;
		}
		CompProperties_Explosive compExplosive = WeaponTool.GetCompExplosive(t);
		if (compExplosive == null)
		{
			CompProperties_Explosive compProperties_Explosive = new CompProperties_Explosive();
			compProperties_Explosive.compClass = typeof(CompProperties_Explosive);
			if (t.comps == null)
			{
				t.comps = new List<CompProperties>();
			}
			t.comps.Add(compProperties_Explosive);
			compProperties_Explosive.ResolveReferences(t);
			t.ResolveReferences();
			t.PostLoad();
		}
	}

	internal static void RemoveCompExplosive(this ThingDef t)
	{
		if (t != null)
		{
			CompProperties_Explosive compExplosive = WeaponTool.GetCompExplosive(t);
			if (compExplosive != null)
			{
				t.comps.Remove(compExplosive);
				t.ResolveReferences();
				t.PostLoad();
			}
		}
	}

	internal static Color GetColor(this ThingDef t, ThingDef stuff)
	{
		if (t == null)
		{
			return Color.white;
		}
		if (stuff != null && t.MadeFromStuff)
		{
			return stuff.stuffProps.color;
		}
		if (t.mineable && t.graphicData != null)
		{
			return t.graphicData.colorTwo;
		}
		if (t.colorGenerator != null)
		{
			return t.colorGenerator.ExemplaryColor;
		}
		if (t.graphicData != null)
		{
			return t.graphicData.color;
		}
		return Color.white;
	}

	internal static void SetDrawColor(this Thing t, Color col)
	{
		t?.TryGetComp<CompColorable>()?.SetColor(col);
	}

	internal static void AddCompQuality(this ThingDef t)
	{
		if (t != null && !t.HasComp(typeof(CompQuality)))
		{
			CompProperties compProperties = new CompProperties();
			compProperties.compClass = typeof(CompQuality);
			if (t.comps == null)
			{
				t.comps = new List<CompProperties>();
			}
			t.comps.Add(compProperties);
			compProperties.ResolveReferences(t);
			t.ResolveReferences();
			t.PostLoad();
		}
	}

	internal static void CopyQuality(this Thing t1, Thing fromT2)
	{
		if (t1 != null && fromT2 != null && fromT2.TryGetQuality(out var qc))
		{
			t1.SetQuality((int)qc);
		}
	}

	internal static void SetQuality(this Thing t, int quali)
	{
		t?.TryGetComp<CompQuality>()?.SetQuality((QualityCategory)quali, ArtGenerationContext.Colony);
	}

	internal static int GetQuality(this Thing t)
	{
		if (t.TryGetQuality(out var qc))
		{
			return (int)qc;
		}
		return 0;
	}

	internal static CompProperties GetCompByType(this ThingDef thingDef, string typeString)
	{
		if (thingDef != null && thingDef.comps != null)
		{
			foreach (CompProperties comp in thingDef.comps)
			{
				if (comp.GetType().ToString() == typeString)
				{
					return comp;
				}
			}
		}
		return null;
	}

	internal static CompProperties GetCompByType(this ThingDef thingDef, Type type)
	{
		if (thingDef != null && thingDef.comps != null)
		{
			foreach (CompProperties comp in thingDef.comps)
			{
				if (comp.GetType() == type)
				{
					return comp;
				}
			}
		}
		return null;
	}

	internal static void Reinvent(this Pawn pawn, Selected selected, int createRandomAmount = 1)
	{
		if (!pawn.HasInventoryTracker())
		{
			return;
		}
		if (selected == null)
		{
			try
			{
				pawn.inventory.DestroyAll();
			}
			catch
			{
			}
			for (int i = 0; i < createRandomAmount; i++)
			{
				Thing t = GenerateRandomItem(null);
				pawn.AddItemToInventory(t);
			}
		}
		else
		{
			Thing t2 = GenerateItem(selected);
			pawn.AddItemToInventory(t2);
		}
	}

	internal static void ReplaceItem(this Pawn pawn, Thing t)
	{
		if (pawn.HasInventoryTracker())
		{
			Thing t2 = GenerateRandomItem(t.def.thingCategories.FirstOrDefault());
			pawn.inventory.innerContainer.Remove(t);
			pawn.AddItemToInventory(t2);
		}
	}

	internal static int GetSilverAmountNear(IntVec3 pos, Map map)
	{
		int num = 0;
		if (map == null)
		{
			return num;
		}
		int x = map.Size.x;
		int z = map.Size.z;
		for (int i = 0; i < x; i++)
		{
			for (int j = 0; j < z; j++)
			{
				IntVec3 c = new IntVec3(i, pos.y, j);
				List<Thing> thingList = c.GetThingList(map);
				foreach (Thing item in thingList)
				{
					if (item?.def == ThingDefOf.Silver)
					{
						num += item.stackCount;
						break;
					}
				}
			}
		}
		return num;
	}

	internal static void ReduceSilverAmount(IntVec3 pos, Map map, int buyPrice)
	{
		if (map == null)
		{
			return;
		}
		int x = map.Size.x;
		int z = map.Size.z;
		for (int i = 0; i < x; i++)
		{
			for (int j = 0; j < z; j++)
			{
				IntVec3 c = new IntVec3(i, pos.y, j);
				List<Thing> thingList = c.GetThingList(map);
				foreach (Thing item in thingList)
				{
					if (item?.def == ThingDefOf.Silver)
					{
						if (item.stackCount > buyPrice)
						{
							item.stackCount -= buyPrice;
							buyPrice = 0;
						}
						else
						{
							buyPrice -= item.stackCount;
							item.Destroy();
						}
						break;
					}
				}
				if (buyPrice <= 0)
				{
					break;
				}
			}
			if (buyPrice <= 0)
			{
				break;
			}
		}
	}

	internal static List<Selected> GrabAllThingsFromMap(Map map, bool doDestroy = true)
	{
		List<Selected> list = new List<Selected>();
		int x = map.Size.x;
		int z = map.Size.z;
		IntVec3 intVec = UI.MouseCell();
		for (int i = 0; i < x; i++)
		{
			for (int j = 0; j < z; j++)
			{
				IntVec3 c = new IntVec3(i, intVec.y, j);
				List<Thing> thingList = c.GetThingList(map);
				if (thingList.NullOrEmpty())
				{
					continue;
				}
				for (int num = thingList.Count - 1; num >= 0; num--)
				{
					Thing thing = thingList[num];
					if (thing != null && thing.def != null && thing.def.category == Verse.ThingCategory.Item && thing.def.building == null)
					{
						list.Add(Selected.ByThing(thing));
						if (doDestroy)
						{
							thing.Destroy();
						}
					}
				}
			}
		}
		return list;
	}

	internal static Thing FindThingOnMap(Selected s, Map map, int amount, bool doDestroy = false)
	{
		if (s.location != default(IntVec3))
		{
			List<Thing> thingList = s.location.GetThingList(map);
			if (!thingList.NullOrEmpty())
			{
				for (int num = thingList.Count - 1; num >= 0; num--)
				{
					Thing thing = thingList[num];
					if (thing != null && thing.def != null && thing.def.defName == s.thingDef.defName && thing.stackCount == amount)
					{
						if (doDestroy)
						{
							thing.Destroy();
						}
						return thing;
					}
				}
			}
		}
		int x = map.Size.x;
		int z = map.Size.z;
		IntVec3 intVec = UI.MouseCell();
		for (int i = 0; i < x; i++)
		{
			for (int j = 0; j < z; j++)
			{
				IntVec3 c = new IntVec3(i, intVec.y, j);
				List<Thing> thingList = c.GetThingList(map);
				if (thingList.NullOrEmpty())
				{
					continue;
				}
				for (int num2 = thingList.Count - 1; num2 >= 0; num2--)
				{
					Thing thing2 = thingList[num2];
					if (thing2 != null && thing2.def != null && thing2.def.defName == s.thingDef.defName && thing2.stackCount == amount)
					{
						if (doDestroy)
						{
							thing2.Destroy();
						}
						return thing2;
					}
				}
			}
		}
		return null;
	}

	internal static Selected GrabThingFromMap(Selected s, Map map, int amount)
	{
		Selected selected = Selected.ByThingDef(s.thingDef);
		selected.stackVal = 0;
		if (map == null || s.thingDef == null)
		{
			return selected;
		}
		int x = map.Size.x;
		int z = map.Size.z;
		IntVec3 intVec = UI.MouseCell();
		for (int i = 0; i < x; i++)
		{
			for (int j = 0; j < z; j++)
			{
				IntVec3 c = new IntVec3(i, intVec.y, j);
				List<Thing> thingList = c.GetThingList(map);
				if (!thingList.NullOrEmpty())
				{
					for (int num = thingList.Count - 1; num >= 0; num--)
					{
						Thing thing = thingList[num];
						if (thing != null && thing.def != null && thing.def.defName == s.thingDef.defName)
						{
							if (thing.stackCount >= amount)
							{
								thing.stackCount -= amount;
								selected.stackVal = amount;
								amount = 0;
							}
							else
							{
								amount -= thing.stackCount;
								selected.stackVal += thing.stackCount;
								thing.Destroy();
							}
							break;
						}
					}
				}
				if (amount <= 0)
				{
					break;
				}
			}
			if (amount <= 0)
			{
				break;
			}
		}
		return selected;
	}

	internal static void BeginBuyItem(Selected selected)
	{
		if (selected == null || selected.thingDef == null)
		{
			return;
		}
		IntVec3 pos = UI.MouseCell();
		int silverAmountNear = GetSilverAmountNear(pos, Find.CurrentMap);
		selected.UpdateBuyPrice();
		MessageTool.Show(Label.SILVER_NEAR + silverAmountNear + Label.SILVER_NEEDED + selected.buyPrice, (silverAmountNear < selected.buyPrice) ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.PositiveEvent);
		if (silverAmountNear >= selected.buyPrice)
		{
			string bUY_ = Label.BUY_1;
			object[] args = new string[2]
			{
				selected.thingDef.label,
				selected.buyPrice.ToString()
			};
			WindowTool.Open(Dialog_MessageBox.CreateConfirmation(string.Format(bUY_, args), delegate
			{
				selected.DmoBuyAndPlaceItem();
			}));
		}
	}

	internal static void DmoBuyAndPlaceItem(this Selected s)
	{
		if (s.thingDef == null)
		{
			return;
		}
		DebugMenuOption debugMenuOption = new DebugMenuOption(Label.PLACE_ITEM, DebugMenuOptionMode.Tool, delegate
		{
			IntVec3 intVec = UI.MouseCell();
			int silverAmountNear = GetSilverAmountNear(intVec, Find.CurrentMap);
			if (silverAmountNear >= s.buyPrice)
			{
				SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
				ReduceSilverAmount(intVec, Find.CurrentMap, s.buyPrice);
				if (s.thingDef.race != null)
				{
					PlacingTool.DoPlaceMultiplePawns(s, CEditor.API.DicFactions.GetValue(CEditor.ListName));
				}
				else if (!s.thingDef.MadeFromStuff && !s.thingDef.HasComp(typeof(CompQuality)))
				{
					DebugThingPlaceHelper.DebugSpawn(s.thingDef, intVec, s.stackVal, direct: false, s.style);
				}
				else
				{
					for (int i = 0; i < s.stackVal; i++)
					{
						Thing thing = ThingMaker.MakeThing(s.thingDef, s.stuff);
						thing.SetQuality(s.quality);
						thing.SetStyleDef(s.style);
						if (thing.CanStackWith(thing))
						{
							GenPlace.TryPlaceThing(thing, UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
						}
						else
						{
							GenPlace.TryPlaceThing(thing.MakeMinified(), UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
						}
					}
				}
				EditWindow_Log.wantsToOpen = false;
			}
			else
			{
				DebugTools.curTool = null;
			}
		});
		DebugTools.curTool = new DebugTool(debugMenuOption.label, debugMenuOption.method);
	}

	internal static void CreateBuilding(string defName, string label, string dsc, Type thingClass, string textur)
	{
		ThingDef thingDef = DefTool.ThingDef(defName);
		if (thingDef != null)
		{
			return;
		}
		try
		{
			thingDef = new ThingDef();
			thingDef.defName = defName;
			thingDef.label = label;
			thingDef.description = dsc;
			thingDef.apparel = null;
			thingDef.designatorDropdown = DefTool.DesignatorDropdownGroupDef("Zombrella");
			thingDef.designationCategory = DefTool.DesignationCategoryDef("Misc");
			thingDef.thingClass = thingClass;
			thingDef.category = Verse.ThingCategory.Building;
			thingDef.altitudeLayer = AltitudeLayer.Building;
			thingDef.passability = Traversability.Impassable;
			thingDef.thingCategories = new List<ThingCategoryDef>();
			thingDef.thingCategories.Add(ThingCategoryDef.Named("BuildingsMisc"));
			ThingCategoryDef.Named("BuildingsMisc").childThingDefs.Add(thingDef);
			thingDef.placeWorkers = new List<Type>();
			thingDef.placeWorkers.Add(typeof(PlaceWorker_Heater));
			thingDef.drawPlaceWorkersWhileSelected = true;
			ThingDef thingDef2 = DefTool.ThingDef(textur, ThingDefOf.Grave);
			thingDef.graphicData = new GraphicData();
			thingDef.graphicData.texPath = thingDef2.graphicData.texPath;
			thingDef.graphicData.graphicClass = typeof(Graphic_Multi);
			if (thingClass == typeof(CharacterEditorGrave))
			{
				thingDef.graphicData.drawSize = new Vector2(3f, 4f);
				thingDef.graphicData.color = new Color(0.4f, 0.4f, 0.5f);
			}
			else
			{
				thingDef.graphicData.drawSize = new Vector2(1f, 2f);
				thingDef.graphicData.color = new Color(0.3f, 0.3f, 0.4f);
			}
			thingDef.graphicData.shadowData = new ShadowData();
			thingDef.graphicData.shadowData.volume = new Vector3(0.83f, 0.3f, 1.7f);
			thingDef.size = new IntVec2(1, 2);
			thingDef.rotatable = true;
			thingDef.selectable = true;
			thingDef.useHitPoints = true;
			thingDef.leaveResourcesWhenKilled = false;
			thingDef.destroyable = true;
			thingDef.blockWind = true;
			thingDef.blockLight = true;
			thingDef.building = new BuildingProperties();
			thingDef.building.claimable = true;
			thingDef.building.alwaysDeconstructible = false;
			if (thingClass == typeof(CharacterEditorGrave))
			{
				thingDef.building.fullGraveGraphicData = new GraphicData();
				thingDef.building.fullGraveGraphicData.texPath = thingDef2.building.fullGraveGraphicData.texPath;
				thingDef.building.fullGraveGraphicData.graphicClass = typeof(Graphic_Multi);
				thingDef.building.fullGraveGraphicData.drawSize = new Vector2(3f, 4f);
				thingDef.building.fullGraveGraphicData.color = new Color(0.4f, 0.4f, 0.5f);
			}
			thingDef.defaultPlacingRot = Rot4.South;
			thingDef.terrainAffordanceNeeded = TerrainAffordanceDefOf.Light;
			if (thingClass != typeof(CharacterEditorGrave))
			{
				thingDef.constructionSkillPrerequisite = 5;
			}
			if (thingClass == typeof(CharacterEditorGrave))
			{
				thingDef.constructEffect = EffecterDefOf.ConstructDirt;
			}
			else
			{
				thingDef.constructEffect = EffecterDefOf.ConstructMetal;
			}
			thingDef.soundImpactDefault = DefTool.SoundDef("BulletImpact_Metal");
			thingDef.repairEffect = EffecterDefOf.ConstructMetal;
			thingDef.hasInteractionCell = true;
			thingDef.interactionCellOffset = new IntVec3(1, 0, 0);
			thingDef.tickerType = TickerType.Normal;
			thingDef.AddStat(StatDefOf.MaxHitPoints, 500f);
			if (thingClass != typeof(CharacterEditorGrave))
			{
				thingDef.AddStat(StatDefOf.MarketValue, 2000f);
			}
			thingDef.AddStat(StatDefOf.WorkToBuild, 5000f);
			thingDef.AddStat(StatDefOf.Flammability, 0.2f);
			thingDef.costList = new List<ThingDefCountClass>();
			thingDef.costList.Add(new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 4));
			if (thingClass == typeof(CharacterEditorGrave))
			{
				thingDef.costList.Add(new ThingDefCountClass(ThingDefOf.WoodLog, 200));
			}
			else
			{
				thingDef.costList.Add(new ThingDefCountClass(ThingDefOf.Steel, 350));
				thingDef.costList.Add(new ThingDefCountClass(ThingDefOf.Plasteel, 50));
			}
			thingDef.modContentPack = thingDef.designatorDropdown.modContentPack;
			if (thingClass == typeof(CharacterEditorGrave))
			{
				thingDef.minifiedDef = null;
			}
			else
			{
				thingDef.minifiedDef = ThingDefOf.MinifiedThing;
			}
			thingDef.RegisterBuildingDef();
			thingDef.designationCategory.ResolveReferences();
			thingDef.designationCategory.PostLoad();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void RegisterBuildingDef(this ThingDef td)
	{
		if (td != null && DefTool.ThingDef(td.defName) == null)
		{
			Type typeFromHandle = typeof(ThingDefGenerator_Buildings);
			object[] param = new object[2] { td, false };
			td.frameDef = (ThingDef)typeFromHandle.CallMethod("NewFrameDef_Thing", param);
			td.frameDef.isFrameInt = true;
			DefTool.GiveShortHashTo(td.frameDef, typeof(ThingDef));
			td.frameDef.ResolveReferences();
			td.frameDef.PostLoad();
			object[] param2 = new object[4] { td, false, null, false };
			td.blueprintDef = (ThingDef)typeFromHandle.CallMethod("NewBlueprintDef_Thing", param2);
			td.blueprintDef.entityDefToBuild = td;
			td.blueprintDef.category = Verse.ThingCategory.Ethereal;
			DefTool.GiveShortHashTo(td.blueprintDef, typeof(ThingDef));
			td.blueprintDef.ResolveReferences();
			td.blueprintDef.PostLoad();
			ThingDef thingDef = null;
			if (td.minifiedDef != null)
			{
				object[] param3 = new object[4] { td, true, td.blueprintDef, false };
				thingDef = (ThingDef)typeFromHandle.CallMethod("NewBlueprintDef_Thing", param3);
				DefTool.GiveShortHashTo(thingDef, typeof(ThingDef));
				thingDef.ResolveReferences();
				thingDef.PostLoad();
			}
			DefTool.GiveShortHashTo(td, typeof(ThingDef));
			td.ResolveReferences();
			td.PostLoad();
			DefDatabase<ThingDef>.Add(td);
			if (td.minifiedDef != null)
			{
				DefDatabase<ThingDef>.Add(thingDef);
			}
			DefDatabase<ThingDef>.Add(td.blueprintDef);
			DefDatabase<ThingDef>.Add(td.frameDef);
		}
	}

	internal static List<string> GetAllApparelTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.IsApparel)
			{
				sortedDictionary.AddFromList(allDef.apparel.tags, 0);
			}
		}
		foreach (PawnKindDef allDef2 in DefDatabase<PawnKindDef>.AllDefs)
		{
			sortedDictionary.AddFromList(allDef2.apparelTags, 0);
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static List<string> GetAllOutfitTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.IsApparel)
			{
				sortedDictionary.AddFromList(allDef.apparel.defaultOutfitTags, 0);
			}
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static List<string> GetAllTradeTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			sortedDictionary.AddFromList(allDef.tradeTags, 0);
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static List<string> GetAllWeaponTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.IsWeapon)
			{
				sortedDictionary.AddFromList(allDef.weaponTags, 0);
			}
		}
		foreach (PawnKindDef allDef2 in DefDatabase<PawnKindDef>.AllDefs)
		{
			sortedDictionary.AddFromList(allDef2.weaponTags, 0);
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static HashSet<int> ListOfQualityInts()
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (QualityCategory allQualityCategory in QualityUtility.AllQualityCategories)
		{
			hashSet.Add((int)allQualityCategory);
		}
		return hashSet;
	}

	internal static HashSet<ThingDef> ListOfItems(string modname, ThingCategoryDef tc, ThingCategory tc2)
	{
		return DefTool.ListBy(DefTool.CONDITION_IS_ITEM(modname, tc, tc2));
	}

	internal static HashSet<ThingDef> ListOfItemsWithNull(string modname, ThingCategoryDef tc, ThingCategory tc2)
	{
		HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
		hashSet.Add(null);
		hashSet.AddRange(ListOfItems(modname, tc, tc2));
		return hashSet;
	}

	internal static ThingStyleDef GetStyle(this ThingDef thingDef, ref HashSet<ThingStyleDef> l, ref int styleIndex, bool random)
	{
		l = new HashSet<ThingStyleDef>();
		if (thingDef == null || !thingDef.CanBeStyled())
		{
			return null;
		}
		l = ListOfThingStyleDefs(thingDef, null, withNull: true);
		if (random)
		{
			styleIndex = CEditor.zufallswert.Next(l.Count);
		}
		if (!l.NullOrEmpty())
		{
			if (l.Count <= styleIndex)
			{
				styleIndex = 0;
			}
			return l.At(styleIndex);
		}
		return null;
	}

	internal static HashSet<ThingStyleDef> ListOfThingStyleDefs(ThingDef thingDef, string modname, bool withNull)
	{
		HashSet<ThingStyleDef> hashSet = new HashSet<ThingStyleDef>();
		if (withNull)
		{
			hashSet.Add(null);
		}
		if (thingDef == null)
		{
			return hashSet;
		}
		bool flag = modname.NullOrEmpty();
		HashSet<StyleCategoryDef> hashSet2 = DefTool.ListByMod<StyleCategoryDef>(modname);
		foreach (StyleCategoryDef item in hashSet2)
		{
			foreach (ThingDefStyle thingDefStyle in item.thingDefStyles)
			{
				if (thingDef == thingDefStyle.ThingDef)
				{
					hashSet.Add(thingDefStyle.StyleDef);
				}
			}
		}
		return hashSet;
	}

	internal static void CopyStatOffsets(this ThingDef t)
	{
		t.equippedStatOffsets.CopyList(ref lCopyStatOffsets);
	}

	internal static void CopyStatFactors(this ThingDef t)
	{
		t.statBases.CopyList(ref lCopyStatFactors);
	}

	internal static void CopyTradeTags(this ThingDef t)
	{
		t.tradeTags.CopyList(ref lCopyTradeTags);
	}

	internal static void CopyWeaponTags(this ThingDef t)
	{
		t.weaponTags.CopyList(ref lCopyWeaponTags);
	}

	internal static void CopyStuffCategories(this ThingDef t)
	{
		t.stuffCategories.CopyList(ref lCopyStuffCategories);
	}

	internal static void CopyBladeLinkTraits(this Thing t)
	{
		CompBladelinkWeapon compBladelinkWeapon = t.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon != null)
		{
			compBladelinkWeapon.TraitsListForReading.CopyList(ref lCopyBladeLinkTraits);
		}
		else
		{
			lCopyBladeLinkTraits = new List<WeaponTraitDef>();
		}
	}

	internal static void CopyCosts(this ThingDef t)
	{
		t.costList.CopyList(ref lCopyCosts);
	}

	internal static void CopyCostsDiff(this ThingDef t)
	{
		t.costListForDifficulty.costList.CopyList(ref lCopyCostsDiff);
	}

	internal static void CopyPrerequisites(this ThingDef t)
	{
		t.researchPrerequisites.CopyList(ref lCopyPrerequisites);
	}

	internal static void CopyApparelLayer(this ThingDef t)
	{
		t.apparel.layers.CopyList(ref lCopyApparelLayer);
	}

	internal static void CopyBodyPartGroup(this ThingDef t)
	{
		t.apparel.bodyPartGroups.CopyList(ref lCopyBodyPartGroup);
	}

	internal static void PasteStatFactors(this ThingDef t)
	{
		if (t.statBases == null)
		{
			t.statBases = new List<StatModifier>();
		}
		t.statBases.PasteListNonDef(lCopyStatFactors, DefTool.CompareStatModifier, DefTool.SetStatModifier, DefTool.DefGetterStatModifier, DefTool.ValGetterStatModifier);
		t.UpdateFreeLists(FreeList.StatFactors);
	}

	internal static void PasteStatOffsets(this ThingDef t)
	{
		if (t.equippedStatOffsets == null)
		{
			t.equippedStatOffsets = new List<StatModifier>();
		}
		t.equippedStatOffsets.PasteListNonDef(lCopyStatOffsets, DefTool.CompareStatModifier, DefTool.SetStatModifier, DefTool.DefGetterStatModifier, DefTool.ValGetterStatModifier);
		t.UpdateFreeLists(FreeList.StatOffsets);
	}

	internal static void PasteStuffCategories(this ThingDef t)
	{
		if (t.stuffCategories == null)
		{
			t.stuffCategories = new List<StuffCategoryDef>();
		}
		t.stuffCategories.PasteList(lCopyStuffCategories);
		t.UpdateFreeLists(FreeList.StuffCategories);
	}

	internal static void PasteBladeLinkTraits(this Thing t)
	{
		t.TryGetComp<CompBladelinkWeapon>()?.TraitsListForReading.PasteList(lCopyBladeLinkTraits);
	}

	internal static void PasteCosts(this ThingDef t)
	{
		if (t.costList == null)
		{
			t.costList = new List<ThingDefCountClass>();
		}
		t.costList.PasteListNonDef(lCopyCosts, DefTool.CompareThingDefCountClass, DefTool.SetThingDefCountClass, DefTool.DefGetterThingDefCountClass, DefTool.ValGetterThingDefCountClass);
		t.UpdateFreeLists(FreeList.Costs);
	}

	internal static void PasteCostsDiff(this ThingDef t)
	{
		if (t.costListForDifficulty.costList == null)
		{
			t.costListForDifficulty.costList = new List<ThingDefCountClass>();
		}
		t.costListForDifficulty.costList.PasteListNonDef(lCopyCostsDiff, DefTool.CompareThingDefCountClass, DefTool.SetThingDefCountClass, DefTool.DefGetterThingDefCountClass, DefTool.ValGetterThingDefCountClass);
		t.UpdateFreeLists(FreeList.CostsDiff);
	}

	internal static void PastePrerequisites(this ThingDef t)
	{
		if (t.researchPrerequisites == null)
		{
			t.researchPrerequisites = new List<ResearchProjectDef>();
		}
		t.researchPrerequisites.PasteList(lCopyPrerequisites);
		t.UpdateFreeLists(FreeList.Prerequisites);
	}

	internal static void PasteApparelLayer(this ThingDef t)
	{
		if (t.apparel.layers == null)
		{
			t.apparel.layers = new List<ApparelLayerDef>();
		}
		t.apparel.layers.PasteList(lCopyApparelLayer);
		t.UpdateFreeLists(FreeList.ApparelLayer);
	}

	internal static void PasteBodyPartGroup(this ThingDef t)
	{
		if (t.apparel.bodyPartGroups == null)
		{
			t.apparel.bodyPartGroups = new List<BodyPartGroupDef>();
		}
		t.apparel.bodyPartGroups.PasteList(lCopyBodyPartGroup);
		t.UpdateFreeLists(FreeList.BodyPartGroup);
	}

	internal static void RemoveStatOffset(this ThingDef t, StatDef def)
	{
		StatModifier statModifier = t.equippedStatOffsets.FindBy(DefTool.CompareStatModifier, def);
		if (statModifier != null)
		{
			t.equippedStatOffsets.Remove(statModifier);
			t.UpdateFreeLists(FreeList.StatOffsets);
		}
	}

	internal static void RemoveStatFactor(this ThingDef t, StatDef def)
	{
		StatModifier statModifier = t.statBases.FindBy(DefTool.CompareStatModifier, def);
		if (statModifier != null)
		{
			t.statBases.Remove(statModifier);
			t.UpdateFreeLists(FreeList.StatFactors);
		}
	}

	internal static void RemoveStuffCategorie(this ThingDef t, StuffCategoryDef def)
	{
		StuffCategoryDef stuffCategoryDef = t.stuffCategories.FindByDef(def);
		if (stuffCategoryDef != null)
		{
			t.stuffCategories.Remove(stuffCategoryDef);
			t.UpdateFreeLists(FreeList.StuffCategories);
		}
	}

	internal static void RemoveBladeLinkTrait(this Thing t, WeaponTraitDef def)
	{
		t.TryGetComp<CompBladelinkWeapon>()?.TraitsListForReading.Remove(def);
	}

	internal static void RemoveCosts(this ThingDef t, ThingDef def)
	{
		ThingDefCountClass thingDefCountClass = t.costList.FindBy(DefTool.CompareThingDefCountClass, def);
		if (thingDefCountClass != null)
		{
			t.costList.Remove(thingDefCountClass);
			t.UpdateFreeLists(FreeList.Costs);
		}
	}

	internal static void RemoveCostsDiff(this ThingDef t, ThingDef def)
	{
		ThingDefCountClass thingDefCountClass = t.costListForDifficulty.costList.FindBy(DefTool.CompareThingDefCountClass, def);
		if (thingDefCountClass != null)
		{
			t.costListForDifficulty.costList.Remove(thingDefCountClass);
			t.UpdateFreeLists(FreeList.CostsDiff);
		}
	}

	internal static void RemovePrerequisite(this ThingDef t, ResearchProjectDef r)
	{
		if (t.researchPrerequisites != null)
		{
			t.researchPrerequisites.Remove(r);
			t.UpdateFreeLists(FreeList.Prerequisites);
		}
	}

	internal static void RemoveApparelLayer(this ThingDef t, ApparelLayerDef a)
	{
		if (t.apparel.layers != null)
		{
			t.apparel.layers.Remove(a);
			t.UpdateFreeLists(FreeList.ApparelLayer);
		}
	}

	internal static void RemoveBodyPartGroup(this ThingDef t, BodyPartGroupDef b)
	{
		if (t.apparel.bodyPartGroups != null)
		{
			t.apparel.bodyPartGroups.Remove(b);
			t.UpdateFreeLists(FreeList.BodyPartGroup);
		}
	}

	internal static void UpdateFreeLists(this ThingDef t, FreeList f)
	{
		if (f == FreeList.All || f == FreeList.CategoryFactors)
		{
			lCategoryDef_Factors = ListOfStatCategoryDef_ForStatFactor(UseAllCategories);
		}
		if (f == FreeList.All || f == FreeList.CategoryOffsets)
		{
			lCategoryDef_Offsets = ListOfStatCategoryDef_ForStatOffset(UseAllCategories);
		}
		if (t == null)
		{
			return;
		}
		t.ResolveReferences();
		if (f == FreeList.All || f == FreeList.StatFactors)
		{
			lFreeStatDefFactors = t.statBases.ListOfFreeCustom(DefTool.StatDefs_Selection((selected_StatFactor_CatDef == null) ? lCategoryDef_Factors : new HashSet<StatCategoryDef> { selected_StatFactor_CatDef }), DefTool.CompareStatModifier, DefTool.CompareStatCategoryNot, selected_StatFactor_CatDef);
		}
		if (f == FreeList.All || f == FreeList.StatOffsets)
		{
			lFreeStatDefOffsets = t.equippedStatOffsets.ListOfFreeCustom(DefTool.StatDefs_Selection((selected_StatOffset_CatDef == null) ? lCategoryDef_Offsets : new HashSet<StatCategoryDef> { selected_StatOffset_CatDef }), DefTool.CompareStatModifier, DefTool.CompareStatCategoryNot, selected_StatOffset_CatDef);
		}
		if (f == FreeList.All || f == FreeList.StuffCategories)
		{
			lFreeStuffCategories = t.stuffCategories.ListOfFreeDef();
		}
		if (f == FreeList.All || f == FreeList.Costs)
		{
			lFreeCosts = t.costList.ListOfFreeCustom<ThingDefCountClass, ThingDef, ThingDef>(CEditor.API.ListOf<ThingDef>(EType.CostItems), DefTool.CompareThingDefCountClass, null, null);
			lFreeCosts = (from d in lFreeCosts
				orderby d.stuffProps?.categories.FirstOrDefault().SDefname(), d.thingCategories?.FirstOrDefault().SDefname(), d.label
				select d).ToHashSet();
		}
		if ((f == FreeList.All || f == FreeList.CostsDiff) && t.costListForDifficulty != null)
		{
			lFreeCostsDiff = t.costListForDifficulty.costList.ListOfFreeCustom<ThingDefCountClass, ThingDef, ThingDef>(CEditor.API.ListOf<ThingDef>(EType.CostItems), DefTool.CompareThingDefCountClass, null, null);
		}
		if (f == FreeList.All || f == FreeList.Prerequisites)
		{
			lFreePrerequisites = t.researchPrerequisites.ListOfFreeDef();
		}
		if ((f == FreeList.All || f == FreeList.ApparelLayer) && t.apparel != null)
		{
			lFreeApparelLayer = t.apparel.layers.ListOfFreeDef();
		}
		if ((f == FreeList.All || f == FreeList.BodyPartGroup) && t.apparel != null)
		{
			lFreeBodyPartGroup = t.apparel.bodyPartGroups.ListOfFreeDef();
		}
		if (f == FreeList.Costs || f == FreeList.Prerequisites || f == FreeList.CostsDiff)
		{
			t.UpdateRecipes();
		}
	}

	internal static void SetResearchPrerequisite(this ThingDef t, ResearchProjectDef r)
	{
		if (t.recipeMaker != null)
		{
			t.recipeMaker.researchPrerequisite = r;
			t.UpdateRecipes();
		}
	}

	internal static void SetStatOffset(this ThingDef t, StatDef s, float offset)
	{
		DefTool.Set(ref t.equippedStatOffsets, DefTool.CompareStatModifier, DefTool.SetStatModifier, s, offset);
		t.UpdateFreeLists(FreeList.StatOffsets);
	}

	internal static void SetStatFactor(this ThingDef t, StatDef s, float factor)
	{
		DefTool.Set(ref t.statBases, DefTool.CompareStatModifier, DefTool.SetStatModifier, s, factor);
		t.UpdateFreeLists(FreeList.StatFactors);
	}

	internal static void SetStuffCategorie(this ThingDef t, StuffCategoryDef cat, Selected s = null)
	{
		if (t.stuffCategories == null)
		{
			t.stuffCategories = new List<StuffCategoryDef>();
		}
		t.stuffCategories.AddDef(cat);
		t.UpdateFreeLists(FreeList.StuffCategories);
		s?.UpdateStuffList();
	}

	internal static void SetBladeLinkTrait(this Thing thing, WeaponTraitDef t)
	{
		thing.TryGetComp<CompBladelinkWeapon>()?.TraitsListForReading.Add(t);
	}

	internal static void SetCosts(this ThingDef t, ThingDef cost, int val)
	{
		DefTool.Set(ref t.costList, DefTool.CompareThingDefCountClass, DefTool.SetThingDefCountClass, cost, val);
		t.UpdateFreeLists(FreeList.Costs);
	}

	internal static void SetCostsDiff(this ThingDef t, ThingDef cost, int val)
	{
		DefTool.Set(ref t.costListForDifficulty.costList, DefTool.CompareThingDefCountClass, DefTool.SetThingDefCountClass, cost, val);
		t.UpdateFreeLists(FreeList.CostsDiff);
	}

	internal static void SetPrerequisite(this ThingDef t, ResearchProjectDef r)
	{
		if (t.researchPrerequisites == null)
		{
			t.researchPrerequisites = new List<ResearchProjectDef>();
		}
		t.researchPrerequisites.Add(r);
		t.UpdateFreeLists(FreeList.Prerequisites);
	}

	internal static void SetApparelLayer(this ThingDef t, ApparelLayerDef a)
	{
		if (t.apparel.layers == null)
		{
			t.apparel.layers = new List<ApparelLayerDef>();
		}
		t.apparel.layers.Add(a);
		t.UpdateFreeLists(FreeList.ApparelLayer);
	}

	internal static void SetBodyPartGroup(this ThingDef t, BodyPartGroupDef b)
	{
		if (t.apparel.bodyPartGroups == null)
		{
			t.apparel.bodyPartGroups = new List<BodyPartGroupDef>();
		}
		t.apparel.bodyPartGroups.Add(b);
		t.UpdateFreeLists(FreeList.BodyPartGroup);
	}

	internal static HashSet<StatCategoryDef> ListOfStatCategoryDef_ForStatFactor(bool all)
	{
		HashSet<StatCategoryDef> lskip = List_StatCategories_ToSkip(all);
		HashSet<StatCategoryDef> hashSet = DefTool.StatCategoryDefs_Selection(lskip);
		hashSet.RemoveCategoriesWithNoStatDef();
		return hashSet;
	}

	internal static HashSet<StatCategoryDef> ListOfStatCategoryDef_ForStatOffset(bool all)
	{
		HashSet<StatCategoryDef> lskip = List_StatCategories_ToSkip(all);
		HashSet<StatCategoryDef> hashSet = DefTool.StatCategoryDefs_Selection(lskip);
		hashSet.RemoveCategoriesWithNoStatDef();
		return hashSet;
	}

	internal static HashSet<StatCategoryDef> List_StatCategories_ToSkip(bool allowAll)
	{
		HashSet<StatCategoryDef> hashSet = new HashSet<StatCategoryDef>();
		if (allowAll)
		{
			return hashSet;
		}
		hashSet.Add(StatCategoryDefOf.Genetics);
		hashSet.Add(StatCategoryDefOf.BasicsNonPawn);
		hashSet.Add(StatCategoryDefOf.Terrain);
		hashSet.Add(StatCategoryDefOf.Apparel);
		hashSet.Add(StatCategoryDefOf.Ability);
		hashSet.Add(StatCategoryDefOf.Meditation);
		hashSet.Add(DefTool.GetDef<StatCategoryDef>("Mechanitor"));
		return hashSet;
	}
}
