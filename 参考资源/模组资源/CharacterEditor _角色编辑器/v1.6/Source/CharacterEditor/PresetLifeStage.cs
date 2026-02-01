using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal class PresetLifeStage
{
	internal enum Param
	{
		P00_defName,
		P01_label,
		P02_reproductive,
		P03_milkable,
		P04_shearable,
		P05_caravanRideable,
		P06_alwaysDowned,
		P07_claimable,
		P08_isInvoluntarySleepNegativeEvent,
		P09_canDoRandomMentalBreaks,
		P10_canSleepWhileHeld,
		P11_canSleepWhenStarving,
		P12_canVoluntarilySleep,
		P13_canInitiateSocialInteraction,
		P14_developmentalStage,
		P15_voxPitch,
		P16_voxVolume,
		P17_healthScaleFactor,
		P18_hungerRateFactor,
		P19_marketValueFactor,
		P20_foodMaxFactor,
		P21_meleeDamageFactor,
		P22_bodySizeFactor,
		P23_equipmentDrawFactor,
		P24_bodyWidth,
		P25_headSizeFactor,
		P26_eyeSizeFactor,
		P27_sittingOffset,
		P28_bodyDrawOffset,
		P29_icon,
		P30_statFactors,
		P31_statOffsets
	}

	internal SortedDictionary<Param, string> dicParams;

	internal LifeStageDef def = null;

	internal static OptionS optionS => OptionS.CUSTOMLIFESTAGE;

	public string AsString => Preset.AsString(dicParams);

	public static Dictionary<string, PresetLifeStage> AllDefaults => CEditor.API.Get<Dictionary<string, PresetLifeStage>>(EType.LifestagePreset);

	public static HashSet<LifeStageDef> ListAll => DefTool.ListBy((LifeStageDef x) => !x.defName.NullOrEmpty());

	public void SaveCustom()
	{
		if (def != null)
		{
			CEditor.API.SetCustom(optionS, AsString, def.defName);
			MessageTool.Show(def.defName + " " + Label.SETTINGSSAVED);
		}
	}

	public static Dictionary<string, PresetLifeStage> CreateDefaults()
	{
		return Preset.CreateDefaults(ListAll, (LifeStageDef sample) => sample.defName, (LifeStageDef x) => new PresetLifeStage(x), "lifestages");
	}

	public static void LoadAllModifications(string custom)
	{
		Preset.LoadAllModifications(custom, delegate(string s)
		{
			new PresetLifeStage(s);
		}, "lifestages");
	}

	public static void ResetAllToDefaults()
	{
		Preset.ResetAllToDefault(AllDefaults, delegate(PresetLifeStage p)
		{
			p.FromDictionary();
		}, optionS, "lifestages");
	}

	public static void ResetToDefault(string defName)
	{
		Preset.ResetToDefault(AllDefaults, delegate(PresetLifeStage p)
		{
			p.FromDictionary();
		}, optionS, defName);
	}

	public static void SaveModification(LifeStageDef s)
	{
		new PresetLifeStage(s).SaveCustom();
	}

	internal PresetLifeStage(LifeStageDef ldef)
	{
		if (ldef == null)
		{
			return;
		}
		def = ldef;
		dicParams = new SortedDictionary<Param, string>();
		try
		{
			dicParams.Add(Param.P00_defName, def.defName);
			dicParams.Add(Param.P01_label, def.label);
			dicParams.Add(Param.P02_reproductive, def.reproductive.ToString());
			dicParams.Add(Param.P03_milkable, def.milkable.ToString());
			dicParams.Add(Param.P04_shearable, def.shearable.ToString());
			dicParams.Add(Param.P05_caravanRideable, def.caravanRideable.ToString());
			dicParams.Add(Param.P06_alwaysDowned, def.alwaysDowned.ToString());
			dicParams.Add(Param.P07_claimable, def.claimable.ToString());
			dicParams.Add(Param.P08_isInvoluntarySleepNegativeEvent, def.involuntarySleepIsNegativeEvent.ToString());
			dicParams.Add(Param.P09_canDoRandomMentalBreaks, def.canDoRandomMentalBreaks.ToString());
			dicParams.Add(Param.P10_canSleepWhileHeld, def.canSleepWhileHeld.ToString());
			dicParams.Add(Param.P11_canSleepWhenStarving, def.canSleepWhenStarving.ToString());
			dicParams.Add(Param.P12_canVoluntarilySleep, def.canVoluntarilySleep.ToString());
			dicParams.Add(Param.P13_canInitiateSocialInteraction, def.canInitiateSocialInteraction.ToString());
			SortedDictionary<Param, string> sortedDictionary = dicParams;
			int developmentalStage = (int)def.developmentalStage;
			sortedDictionary.Add(Param.P14_developmentalStage, developmentalStage.ToString());
			dicParams.Add(Param.P15_voxPitch, def.voxPitch.ToString());
			dicParams.Add(Param.P16_voxVolume, def.voxVolume.ToString());
			dicParams.Add(Param.P17_healthScaleFactor, def.healthScaleFactor.ToString());
			dicParams.Add(Param.P18_hungerRateFactor, def.hungerRateFactor.ToString());
			dicParams.Add(Param.P19_marketValueFactor, def.marketValueFactor.ToString());
			dicParams.Add(Param.P20_foodMaxFactor, def.foodMaxFactor.ToString());
			dicParams.Add(Param.P21_meleeDamageFactor, def.meleeDamageFactor.ToString());
			dicParams.Add(Param.P22_bodySizeFactor, def.bodySizeFactor.ToString());
			dicParams.Add(Param.P23_equipmentDrawFactor, def.equipmentDrawDistanceFactor.ToString());
			dicParams.Add(Param.P24_bodyWidth, def.bodyWidth.ToString());
			dicParams.Add(Param.P25_headSizeFactor, def.headSizeFactor.ToString());
			dicParams.Add(Param.P26_eyeSizeFactor, def.eyeSizeFactor.ToString());
			dicParams.Add(Param.P27_sittingOffset, def.sittingOffset.ToString());
			dicParams.Add(Param.P28_bodyDrawOffset, def.bodyDrawOffset.AsString());
			dicParams.Add(Param.P29_icon, def.icon);
			dicParams.Add(Param.P30_statFactors, def.statFactors.ListToString());
			dicParams.Add(Param.P31_statOffsets, def.statOffsets.ListToString());
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal PresetLifeStage(string custom)
	{
		if (Preset.LoadModification(custom, ref dicParams) && FromDictionary())
		{
			Log.Message(def.defName + " " + Label.MODIFICATIONLOADED);
		}
	}

	public bool FromDictionary()
	{
		def = DefTool.GetDef<LifeStageDef>(dicParams.GetValue(Param.P00_defName));
		if (def == null)
		{
			return false;
		}
		try
		{
			def.label = dicParams.GetValue(Param.P01_label);
			def.reproductive = dicParams.GetValue(Param.P02_reproductive).AsBool();
			def.milkable = dicParams.GetValue(Param.P03_milkable).AsBool();
			def.shearable = dicParams.GetValue(Param.P04_shearable).AsBool();
			def.caravanRideable = dicParams.GetValue(Param.P05_caravanRideable).AsBool();
			def.alwaysDowned = dicParams.GetValue(Param.P06_alwaysDowned).AsBool();
			def.claimable = dicParams.GetValue(Param.P07_claimable).AsBool();
			def.involuntarySleepIsNegativeEvent = dicParams.GetValue(Param.P08_isInvoluntarySleepNegativeEvent).AsBool();
			def.canDoRandomMentalBreaks = dicParams.GetValue(Param.P09_canDoRandomMentalBreaks).AsBool();
			def.canSleepWhileHeld = dicParams.GetValue(Param.P10_canSleepWhileHeld).AsBool();
			def.canSleepWhenStarving = dicParams.GetValue(Param.P11_canSleepWhenStarving).AsBool();
			def.canVoluntarilySleep = dicParams.GetValue(Param.P12_canVoluntarilySleep).AsBool();
			def.canInitiateSocialInteraction = dicParams.GetValue(Param.P13_canInitiateSocialInteraction).AsBool();
			if (Enum.TryParse<DevelopmentalStage>(dicParams.GetValue(Param.P14_developmentalStage), out var result))
			{
				def.developmentalStage = result;
			}
			def.voxPitch = dicParams.GetValue(Param.P15_voxPitch).AsFloat();
			def.voxVolume = dicParams.GetValue(Param.P16_voxVolume).AsFloat();
			def.healthScaleFactor = dicParams.GetValue(Param.P17_healthScaleFactor).AsFloat();
			def.hungerRateFactor = dicParams.GetValue(Param.P18_hungerRateFactor).AsFloat();
			def.marketValueFactor = dicParams.GetValue(Param.P19_marketValueFactor).AsFloat();
			def.foodMaxFactor = dicParams.GetValue(Param.P20_foodMaxFactor).AsFloat();
			def.meleeDamageFactor = dicParams.GetValue(Param.P21_meleeDamageFactor).AsFloat();
			def.bodySizeFactor = dicParams.GetValue(Param.P22_bodySizeFactor).AsFloat();
			def.equipmentDrawDistanceFactor = dicParams.GetValue(Param.P23_equipmentDrawFactor).AsFloat();
			def.bodyWidth = dicParams.GetValue(Param.P24_bodyWidth).AsFloatZero();
			def.headSizeFactor = dicParams.GetValue(Param.P25_headSizeFactor).AsFloatZero();
			def.eyeSizeFactor = dicParams.GetValue(Param.P26_eyeSizeFactor).AsFloatZero();
			def.sittingOffset = dicParams.GetValue(Param.P27_sittingOffset).AsFloatZero();
			def.bodyDrawOffset = dicParams.GetValue(Param.P28_bodyDrawOffset).AsVector3NonZero();
			def.icon = dicParams.GetValue(Param.P29_icon);
			def.statFactors = dicParams.GetValue(Param.P30_statFactors).StringToListNonDef<StatModifier>();
			def.statOffsets = dicParams.GetValue(Param.P31_statOffsets).StringToListNonDef<StatModifier>();
			def.ResolveReferences();
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
			return false;
		}
		return true;
	}
}
