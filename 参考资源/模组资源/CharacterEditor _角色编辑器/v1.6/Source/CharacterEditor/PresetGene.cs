using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal class PresetGene
{
	internal enum Param
	{
		P00_defName,
		P01_label,
		P02_statFactors,
		P03_statOffsets,
		P04_aptitudes,
		P05_capacities,
		P06_abilities,
		P07_traits,
		P08_suppressedTraits,
		P09_immunities,
		P10_protections,
		P11_disabledNeeds,
		P12_disabledWorkTags,
		P13_damageFactors,
		P14_causedNeeds,
		P15_chemical,
		P16_forcedHair,
		P17_hairColorOverride,
		P18_skinColorBase,
		P19_skinColorOverride,
		P20_biostatArc,
		P21_biostatCpx,
		P22_biostatMet,
		P23_addictionChanceFactor,
		P24_mentalBreakChanceFactor,
		P25_foodPoisioningChanceFactor,
		P26_lovinMTBFactor,
		P27_marketValueFactor,
		P28_mentalBreakMtbDays,
		P29_minAgeActive,
		P30_minMelanin,
		P31_missingGeneRomanceChanceFactor,
		P32_painFactor,
		P33_painOffset,
		P34_prisonBreakMtbFactor,
		P35_randomBrightnessFactor,
		P36_resourceLossPerDay,
		P37_selectionWeight,
		P38_selectionWeightFactorDarkSkin,
		P39_socialFightChanceFactor,
		P40_canGenerateInGeneSet,
		P41_dislikesSunLight,
		P42_dontMindRawFood,
		P43_ignoreDarkness,
		P44_immuneToToxGasExposure,
		P45_neverGrayHair,
		P46_prevenetPermanent_Wounds,
		P47_randomChosen,
		P48_removeOnRedress,
		P49_showGizmoOnWorldView,
		P50_sterilize,
		P51_womenCanHaveBeards,
		P52_overdoseChanceFactor,
		P53_toleranceBuildupFactor,
		P54_displayOrderInCategory,
		P55_passOnDirectly,
		P56_showGizmoWhenDrafted,
		P57_showGizmoOnMultiSelect,
		P58_soundCall,
		P59_soundDeath,
		P60_soundWounded,
		P61_historyEventDef,
		P62_prerequisiteDef,
		P63_labelAdj,
		P64_iconPath,
		P65_resourceLabel,
		P66_resourceDescription,
		P67_geneCategory,
		P68_endogeneCategory,
		P69_passionMod,
		P70_geneticBodyType,
		P71_customEffectDescriptions,
		P72_resourceGizmoThresholds,
		P73_forcedHeadTypes,
		P74_exclusionTags,
		P75_hairTagFilter,
		P76_beardTagFilter
	}

	internal SortedDictionary<Param, string> dicParams;

	internal GeneDef def = null;

	internal static OptionS optionS => OptionS.CUSTOMGENE;

	public string AsString => Preset.AsString(dicParams);

	public static Dictionary<string, PresetGene> AllDefaults => CEditor.API.Get<Dictionary<string, PresetGene>>(EType.GenePreset);

	public static HashSet<GeneDef> ListAll => DefTool.ListBy((GeneDef x) => !x.defName.NullOrEmpty());

	public void SaveCustom()
	{
		if (def != null)
		{
			CEditor.API.SetCustom(optionS, AsString, def.defName);
			MessageTool.Show(def.defName + " " + Label.SETTINGSSAVED);
		}
	}

	public static Dictionary<string, PresetGene> CreateDefaults()
	{
		return Preset.CreateDefaults(ListAll, (GeneDef sample) => sample.defName, (GeneDef x) => new PresetGene(x), "genes");
	}

	public static void LoadAllModifications(string custom)
	{
		Preset.LoadAllModifications(custom, delegate(string s)
		{
			new PresetGene(s);
		}, "genes");
	}

	public static void ResetAllToDefaults()
	{
		Preset.ResetAllToDefault(AllDefaults, delegate(PresetGene p)
		{
			p.FromDictionary();
		}, optionS, "genes");
	}

	public static void ResetToDefault(string defName)
	{
		Preset.ResetToDefault(AllDefaults, delegate(PresetGene p)
		{
			p.FromDictionary();
		}, optionS, defName);
	}

	public static void SaveModification(GeneDef s)
	{
		new PresetGene(s).SaveCustom();
	}

	internal PresetGene(GeneDef g)
	{
		if (g == null)
		{
			return;
		}
		def = g;
		dicParams = new SortedDictionary<Param, string>();
		try
		{
			dicParams.Add(Param.P00_defName, def.defName);
			dicParams.Add(Param.P01_label, def.label);
			dicParams.Add(Param.P02_statFactors, def.statFactors.ListToString());
			dicParams.Add(Param.P03_statOffsets, def.statOffsets.ListToString());
			dicParams.Add(Param.P04_aptitudes, def.aptitudes.ListToString());
			dicParams.Add(Param.P05_capacities, def.capMods.ListToString());
			dicParams.Add(Param.P06_abilities, def.abilities.ListToString());
			dicParams.Add(Param.P07_traits, def.forcedTraits.ListToString());
			dicParams.Add(Param.P08_suppressedTraits, def.suppressedTraits.ListToString());
			dicParams.Add(Param.P09_immunities, def.makeImmuneTo.ListToString());
			dicParams.Add(Param.P10_protections, def.hediffGiversCannotGive.ListToString());
			dicParams.Add(Param.P11_disabledNeeds, def.disablesNeeds.ListToString());
			SortedDictionary<Param, string> sortedDictionary = dicParams;
			int disabledWorkTags = (int)def.disabledWorkTags;
			sortedDictionary.Add(Param.P12_disabledWorkTags, disabledWorkTags.ToString());
			dicParams.Add(Param.P13_damageFactors, def.damageFactors.ListToString());
			dicParams.Add(Param.P14_causedNeeds, def.enablesNeeds.NullOrEmpty() ? "" : def.enablesNeeds[0].SDefname());
			dicParams.Add(Param.P15_chemical, def.chemical.SDefname());
			dicParams.Add(Param.P16_forcedHair, def.forcedHair.SDefname());
			dicParams.Add(Param.P17_hairColorOverride, def.hairColorOverride.NullableColorHexString());
			dicParams.Add(Param.P18_skinColorBase, def.skinColorBase.NullableColorHexString());
			dicParams.Add(Param.P19_skinColorOverride, def.skinColorOverride.NullableColorHexString());
			dicParams.Add(Param.P20_biostatArc, def.biostatArc.ToString());
			dicParams.Add(Param.P21_biostatCpx, def.biostatCpx.ToString());
			dicParams.Add(Param.P22_biostatMet, def.biostatMet.ToString());
			dicParams.Add(Param.P23_addictionChanceFactor, def.addictionChanceFactor.ToString());
			dicParams.Add(Param.P24_mentalBreakChanceFactor, def.aggroMentalBreakSelectionChanceFactor.ToString());
			dicParams.Add(Param.P25_foodPoisioningChanceFactor, def.foodPoisoningChanceFactor.ToString());
			dicParams.Add(Param.P26_lovinMTBFactor, def.lovinMTBFactor.ToString());
			dicParams.Add(Param.P27_marketValueFactor, def.marketValueFactor.ToString());
			dicParams.Add(Param.P28_mentalBreakMtbDays, def.mentalBreakMtbDays.ToString());
			dicParams.Add(Param.P29_minAgeActive, def.minAgeActive.ToString());
			dicParams.Add(Param.P30_minMelanin, def.minMelanin.ToString());
			dicParams.Add(Param.P31_missingGeneRomanceChanceFactor, def.missingGeneRomanceChanceFactor.ToString());
			dicParams.Add(Param.P32_painFactor, def.painFactor.ToString());
			dicParams.Add(Param.P33_painOffset, def.painOffset.ToString());
			dicParams.Add(Param.P34_prisonBreakMtbFactor, def.prisonBreakMTBFactor.ToString());
			dicParams.Add(Param.P35_randomBrightnessFactor, def.randomBrightnessFactor.ToString());
			dicParams.Add(Param.P36_resourceLossPerDay, def.resourceLossPerDay.ToString());
			dicParams.Add(Param.P37_selectionWeight, def.selectionWeight.ToString());
			dicParams.Add(Param.P38_selectionWeightFactorDarkSkin, def.selectionWeightFactorDarkSkin.ToString());
			dicParams.Add(Param.P39_socialFightChanceFactor, def.socialFightChanceFactor.ToString());
			dicParams.Add(Param.P40_canGenerateInGeneSet, def.canGenerateInGeneSet.ToString());
			dicParams.Add(Param.P41_dislikesSunLight, def.dislikesSunlight.ToString());
			dicParams.Add(Param.P42_dontMindRawFood, def.dontMindRawFood.ToString());
			dicParams.Add(Param.P43_ignoreDarkness, def.ignoreDarkness.ToString());
			dicParams.Add(Param.P44_immuneToToxGasExposure, def.immuneToToxGasExposure.ToString());
			dicParams.Add(Param.P45_neverGrayHair, def.neverGrayHair.ToString());
			dicParams.Add(Param.P46_prevenetPermanent_Wounds, def.preventPermanentWounds.ToString());
			dicParams.Add(Param.P47_randomChosen, def.randomChosen.ToString());
			dicParams.Add(Param.P48_removeOnRedress, def.removeOnRedress.ToString());
			dicParams.Add(Param.P49_showGizmoOnWorldView, def.showGizmoOnWorldView.ToString());
			dicParams.Add(Param.P50_sterilize, def.sterilize.ToString());
			dicParams.Add(Param.P51_womenCanHaveBeards, def.womenCanHaveBeards.ToString());
			dicParams.Add(Param.P52_overdoseChanceFactor, def.overdoseChanceFactor.ToString());
			dicParams.Add(Param.P53_toleranceBuildupFactor, def.toleranceBuildupFactor.ToString());
			dicParams.Add(Param.P54_displayOrderInCategory, def.displayOrderInCategory.ToString());
			dicParams.Add(Param.P55_passOnDirectly, def.passOnDirectly.ToString());
			dicParams.Add(Param.P56_showGizmoWhenDrafted, def.showGizmoWhenDrafted.ToString());
			dicParams.Add(Param.P57_showGizmoOnMultiSelect, def.showGizmoOnMultiSelect.ToString());
			dicParams.Add(Param.P58_soundCall, def.soundCall.SDefname());
			dicParams.Add(Param.P59_soundDeath, def.soundDeath.SDefname());
			dicParams.Add(Param.P60_soundWounded, def.soundWounded.SDefname());
			dicParams.Add(Param.P61_historyEventDef, def.deathHistoryEvent.SDefname());
			dicParams.Add(Param.P62_prerequisiteDef, def.prerequisite.SDefname());
			dicParams.Add(Param.P63_labelAdj, def.labelShortAdj);
			dicParams.Add(Param.P64_iconPath, def.iconPath);
			dicParams.Add(Param.P65_resourceLabel, def.resourceLabel);
			dicParams.Add(Param.P66_resourceDescription, def.resourceDescription);
			dicParams.Add(Param.P67_geneCategory, def.displayCategory.SDefname());
			SortedDictionary<Param, string> sortedDictionary2 = dicParams;
			disabledWorkTags = (int)def.endogeneCategory;
			sortedDictionary2.Add(Param.P68_endogeneCategory, disabledWorkTags.ToString());
			dicParams.Add(Param.P69_passionMod, def.passionMod.AsListString());
			dicParams.Add(Param.P70_geneticBodyType, (!def.bodyType.HasValue) ? "" : def.bodyType.ToString());
			dicParams.Add(Param.P71_customEffectDescriptions, def.customEffectDescriptions.ListToString());
			dicParams.Add(Param.P72_resourceGizmoThresholds, def.resourceGizmoThresholds.ListToString());
			dicParams.Add(Param.P73_forcedHeadTypes, def.forcedHeadTypes.ListToString());
			dicParams.Add(Param.P74_exclusionTags, def.exclusionTags.ListToString());
			dicParams.Add(Param.P75_hairTagFilter, def.hairTagFilter.AsListString());
			dicParams.Add(Param.P76_beardTagFilter, def.beardTagFilter.AsListString());
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal PresetGene(string custom)
	{
		if (Preset.LoadModification(custom, ref dicParams) && FromDictionary())
		{
			Log.Message(def.defName + " " + Label.MODIFICATIONLOADED);
		}
	}

	private bool FromDictionary()
	{
		def = DefTool.GetDef<GeneDef>(dicParams.GetValue(Param.P00_defName));
		if (def == null)
		{
			return false;
		}
		try
		{
			def.label = dicParams.GetValue(Param.P01_label);
			def.statFactors = dicParams.GetValue(Param.P02_statFactors).StringToListNonDef<StatModifier>();
			def.statOffsets = dicParams.GetValue(Param.P03_statOffsets).StringToListNonDef<StatModifier>();
			def.aptitudes = dicParams.GetValue(Param.P04_aptitudes).StringToListNonDef<Aptitude>();
			def.capMods = dicParams.GetValue(Param.P05_capacities).StringToListNonDef<PawnCapacityModifier>();
			def.abilities = dicParams.GetValue(Param.P06_abilities).StringToList<AbilityDef>();
			def.forcedTraits = dicParams.GetValue(Param.P07_traits).StringToListNonDef<GeneticTraitData>();
			def.suppressedTraits = dicParams.GetValue(Param.P08_suppressedTraits).StringToListNonDef<GeneticTraitData>();
			def.makeImmuneTo = dicParams.GetValue(Param.P09_immunities).StringToList<HediffDef>();
			def.hediffGiversCannotGive = dicParams.GetValue(Param.P10_protections).StringToList<HediffDef>();
			def.disablesNeeds = dicParams.GetValue(Param.P11_disabledNeeds).StringToList<NeedDef>();
			if (Enum.TryParse<WorkTags>(dicParams.GetValue(Param.P12_disabledWorkTags), out var result))
			{
				def.disabledWorkTags = result;
			}
			def.damageFactors = dicParams.GetValue(Param.P13_damageFactors).StringToListNonDef<DamageFactor>();
			def.chemical = DefTool.GetDef<ChemicalDef>(dicParams.GetValue(Param.P15_chemical));
			def.forcedHair = DefTool.GetDef<HairDef>(dicParams.GetValue(Param.P16_forcedHair));
			def.hairColorOverride = dicParams.GetValue(Param.P17_hairColorOverride).HexStringToColorNullable();
			def.skinColorBase = dicParams.GetValue(Param.P18_skinColorBase).HexStringToColorNullable();
			def.skinColorOverride = dicParams.GetValue(Param.P19_skinColorOverride).HexStringToColorNullable();
			def.biostatArc = dicParams.GetValue(Param.P20_biostatArc).AsInt32();
			def.biostatCpx = dicParams.GetValue(Param.P21_biostatCpx).AsInt32();
			def.biostatMet = dicParams.GetValue(Param.P22_biostatMet).AsInt32();
			def.addictionChanceFactor = dicParams.GetValue(Param.P23_addictionChanceFactor).AsFloat();
			def.aggroMentalBreakSelectionChanceFactor = dicParams.GetValue(Param.P24_mentalBreakChanceFactor).AsFloat();
			def.foodPoisoningChanceFactor = dicParams.GetValue(Param.P25_foodPoisioningChanceFactor).AsFloat();
			def.lovinMTBFactor = dicParams.GetValue(Param.P26_lovinMTBFactor).AsFloat();
			def.marketValueFactor = dicParams.GetValue(Param.P27_marketValueFactor).AsFloat();
			def.mentalBreakMtbDays = dicParams.GetValue(Param.P28_mentalBreakMtbDays).AsFloat();
			def.minAgeActive = dicParams.GetValue(Param.P29_minAgeActive).AsFloat();
			def.minMelanin = dicParams.GetValue(Param.P30_minMelanin).AsFloat();
			def.missingGeneRomanceChanceFactor = dicParams.GetValue(Param.P31_missingGeneRomanceChanceFactor).AsFloat();
			def.painFactor = dicParams.GetValue(Param.P32_painFactor).AsFloat();
			def.painOffset = dicParams.GetValue(Param.P33_painOffset).AsFloat();
			def.prisonBreakMTBFactor = dicParams.GetValue(Param.P34_prisonBreakMtbFactor).AsFloat();
			def.randomBrightnessFactor = dicParams.GetValue(Param.P35_randomBrightnessFactor).AsFloat();
			def.resourceLossPerDay = dicParams.GetValue(Param.P36_resourceLossPerDay).AsFloat();
			def.selectionWeight = dicParams.GetValue(Param.P37_selectionWeight).AsFloat();
			def.selectionWeightFactorDarkSkin = dicParams.GetValue(Param.P38_selectionWeightFactorDarkSkin).AsFloat();
			def.socialFightChanceFactor = dicParams.GetValue(Param.P39_socialFightChanceFactor).AsFloat();
			def.canGenerateInGeneSet = dicParams.GetValue(Param.P40_canGenerateInGeneSet).AsBool();
			def.dislikesSunlight = dicParams.GetValue(Param.P41_dislikesSunLight).AsBool();
			def.dontMindRawFood = dicParams.GetValue(Param.P42_dontMindRawFood).AsBool();
			def.ignoreDarkness = dicParams.GetValue(Param.P43_ignoreDarkness).AsBool();
			def.immuneToToxGasExposure = dicParams.GetValue(Param.P44_immuneToToxGasExposure).AsBool();
			def.neverGrayHair = dicParams.GetValue(Param.P45_neverGrayHair).AsBool();
			def.preventPermanentWounds = dicParams.GetValue(Param.P46_prevenetPermanent_Wounds).AsBool();
			def.randomChosen = dicParams.GetValue(Param.P47_randomChosen).AsBool();
			def.removeOnRedress = dicParams.GetValue(Param.P48_removeOnRedress).AsBool();
			def.showGizmoOnWorldView = dicParams.GetValue(Param.P49_showGizmoOnWorldView).AsBool();
			def.sterilize = dicParams.GetValue(Param.P50_sterilize).AsBool();
			def.womenCanHaveBeards = dicParams.GetValue(Param.P51_womenCanHaveBeards).AsBool();
			def.overdoseChanceFactor = dicParams.GetValue(Param.P52_overdoseChanceFactor).AsFloat();
			def.toleranceBuildupFactor = dicParams.GetValue(Param.P53_toleranceBuildupFactor).AsFloat();
			def.displayOrderInCategory = dicParams.GetValue(Param.P54_displayOrderInCategory).AsFloat();
			def.passOnDirectly = dicParams.GetValue(Param.P55_passOnDirectly).AsBool();
			def.showGizmoWhenDrafted = dicParams.GetValue(Param.P56_showGizmoWhenDrafted).AsBool();
			def.showGizmoOnMultiSelect = dicParams.GetValue(Param.P57_showGizmoOnMultiSelect).AsBool();
			def.soundCall = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P58_soundCall));
			def.soundDeath = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P59_soundDeath));
			def.soundWounded = DefTool.GetDef<SoundDef>(dicParams.GetValue(Param.P60_soundWounded));
			def.deathHistoryEvent = DefTool.GetDef<HistoryEventDef>(dicParams.GetValue(Param.P61_historyEventDef));
			def.prerequisite = DefTool.GetDef<GeneDef>(dicParams.GetValue(Param.P62_prerequisiteDef));
			def.labelShortAdj = dicParams.GetValue(Param.P63_labelAdj);
			def.iconPath = dicParams.GetValue(Param.P64_iconPath);
			def.resourceLabel = dicParams.GetValue(Param.P65_resourceLabel);
			def.resourceDescription = dicParams.GetValue(Param.P66_resourceDescription);
			def.displayCategory = DefTool.GetDef<GeneCategoryDef>(dicParams.GetValue(Param.P67_geneCategory));
			if (Enum.TryParse<EndogeneCategory>(dicParams.GetValue(Param.P68_endogeneCategory), out var result2))
			{
				def.endogeneCategory = result2;
			}
			def.passionMod = dicParams.GetValue(Param.P69_passionMod).StringToListNonDef<PassionMod>().FirstOrFallback();
			if (Enum.TryParse<GeneticBodyType>(dicParams.GetValue(Param.P70_geneticBodyType), out var result3))
			{
				def.bodyType = result3;
			}
			def.customEffectDescriptions = dicParams.GetValue(Param.P71_customEffectDescriptions).StringToList();
			def.resourceGizmoThresholds = dicParams.GetValue(Param.P72_resourceGizmoThresholds).StringToFList();
			def.forcedHeadTypes = dicParams.GetValue(Param.P73_forcedHeadTypes).StringToList<HeadTypeDef>();
			def.exclusionTags = dicParams.GetValue(Param.P74_exclusionTags).StringToList();
			def.hairTagFilter = dicParams.GetValue(Param.P75_hairTagFilter).StringToTagFilter();
			def.beardTagFilter = dicParams.GetValue(Param.P76_beardTagFilter).StringToTagFilter();
			def.ResolveReferences();
			def.DoAllGeneActions();
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
			return false;
		}
		return true;
	}
}
