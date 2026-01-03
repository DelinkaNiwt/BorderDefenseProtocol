using System;
using System.Collections.Generic;
using AncotLibrary;
using UnityEngine;
using Verse;

namespace Milira;

public class MiliraRaceSettings : ModSettings
{
	private static Vector2 scrollPosition = Vector2.zero;

	public static bool MiliraRace_ModSetting_StoryOverall = true;

	public static bool MiliraRace_ModSetting_MilianClusterInMap = true;

	public static bool MiliraRace_ModSetting_MilianSmallClusterInMap = true;

	public static bool MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality = false;

	public static bool MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial = false;

	public static bool MiliraRace_ModSetting_MilianDifficulty_Promotion = true;

	public static bool MiliraRace_ModSetting_MilianDifficulty_FastPromotion = false;

	public static bool MiliraRace_ModSetting_MilianDifficulty_WidePromotion = false;

	public static bool MiliraRace_ModSetting_MilianDifficulty_ClusterResonator = true;

	public static bool MiliraRace_ModSetting_MilianDifficulty_ClusterFortress = false;

	public static bool MiliraRace_ModSetting_MiliraDifficulty_TirelessFly = false;

	public static bool MiliraRace_ModSetting_RaceRestrictedApparel = true;

	public static bool MiliraRace_ModSetting_MilianConfigTab = true;

	public static bool MiliraRace_ModSetting_MilianHairColor = false;

	public static bool MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride = true;

	public static bool MiliraRace_ModSetting_MilianHairColorOffset = false;

	public static bool MiliraRace_ModSetting_MilianDrawHeadgear = true;

	public static bool MiliraRace_ModSetting_MilianDisableWorksGestate = false;

	public static MiliraDifficultyScale currentGameDifficulty = MiliraDifficultyScale.Easy;

	public static TabAvailable TabAvailable_MilianConfig = (TabAvailable)2;

	public static TabAvailable TabAvailable_MilianWork = (TabAvailable)0;

	public static MilianModificationChanceScale currentMilianModifyScale = MilianModificationChanceScale.Normal;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref MiliraRace_ModSetting_StoryOverall, "MiliraRace_ModSetting_StoryOverall", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianClusterInMap, "MiliraRace_ModSetting_MilianClusterInMap", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianSmallClusterInMap, "MiliraRace_ModSetting_MilianSmallClusterInMap", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality, "MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial, "MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_Promotion, "MiliraRace_ModSetting_MilianDifficulty_Promotion", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_FastPromotion, "MiliraRace_ModSetting_MilianDifficulty_FastPromotion", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_WidePromotion, "MiliraRace_ModSetting_MilianDifficulty_WidePromotion", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_ClusterResonator, "MiliraRace_ModSetting_MilianDifficulty_ClusterResonator", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDifficulty_ClusterFortress, "MiliraRace_ModSetting_MilianDifficulty_ClusterFortress", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MiliraDifficulty_TirelessFly, "MiliraRace_ModSetting_MiliraDifficulty_TirelessFly", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_RaceRestrictedApparel, "MiliraRace_ModSetting_RaceRestrictedApparel", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianConfigTab, "MiliraRace_ModSetting_MilianConfigTab", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianHairColor, "MiliraRace_ModSetting_MilianHairColor", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride, "MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianHairColorOffset, "MiliraRace_ModSetting_MilianHairColorOffset", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDrawHeadgear, "MiliraRace_ModSetting_MilianDrawHeadgear", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref MiliraRace_ModSetting_MilianDisableWorksGestate, "MiliraRace_ModSetting_MilianDisableWorksGestate", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref currentGameDifficulty, "currentGameDifficulty", MiliraDifficultyScale.Easy, forceSave: true);
		Scribe_Values.Look(ref TabAvailable_MilianConfig, "TabAvailable_MilianConfig", (TabAvailable)2, forceSave: true);
		Scribe_Values.Look(ref TabAvailable_MilianWork, "TabAvailable_MilianWork", (TabAvailable)0, forceSave: true);
		Scribe_Values.Look(ref currentMilianModifyScale, "currentMilianModifyScale", MilianModificationChanceScale.Normal, forceSave: true);
	}

	public static void DoWindowContents(Rect rect)
	{
		//IL_05b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
		Rect rect2 = new Rect(rect.x, rect.y, rect.width - 20f, 850f);
		Widgets.BeginScrollView(rect, ref scrollPosition, rect2);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect2);
		listing_Standard.Gap(5f);
		Text.Font = GameFont.Medium;
		listing_Standard.Label("Milira.ModSetting_EventSwitch".Translate());
		Text.Font = GameFont.Small;
		listing_Standard.GapLine();
		listing_Standard.CheckboxLabeled("MiliraRaceSetting_StoryOverall_Label".Translate(), ref MiliraRace_ModSetting_StoryOverall, "MiliraRaceSetting_StoryOverall_Desc".Translate());
		if (MiliraRace_ModSetting_StoryOverall)
		{
		}
		listing_Standard.CheckboxLabeled("MiliraRaceSetting_MilianClusterInMap_Label".Translate(), ref MiliraRace_ModSetting_MilianClusterInMap, "MiliraRaceSetting_MilianClusterInMap_Desc".Translate());
		listing_Standard.CheckboxLabeled("MiliraRaceSetting_MilianSmallClusterInMap_Label".Translate(), ref MiliraRace_ModSetting_MilianSmallClusterInMap, "MiliraRaceSetting_MilianSmallClusterInMap_Desc".Translate());
		Text.Font = GameFont.Medium;
		listing_Standard.Label("Milira.ModSetting_DifficultyAdjustment".Translate());
		Text.Font = GameFont.Small;
		listing_Standard.GapLine();
		string desc;
		if (listing_Standard.ButtonTextLabeledPct("Milira.DifficultyScaleSetting".Translate() + ": ", CurrentDifficultyScaleLabel(currentGameDifficulty), 0.8f, TextAnchor.MiddleLeft, null, CurrentDifficultyScaleDesc(currentGameDifficulty)))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (MiliraDifficultyScale scale in Enum.GetValues(typeof(MiliraDifficultyScale)))
			{
				DifficultyScaleInfo(scale, out var label, out desc);
				list.Add(new FloatMenuOption(label, delegate
				{
					DifficultySettingTo(scale);
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality, "MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality_Desc".Translate());
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial, "MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial_Desc".Translate());
		if (ModsConfig.IsActive("Ancot.MilianModification") && listing_Standard.ButtonTextLabeledPct("Milira.MilianModifySetting".Translate() + ": ", MilianModifySetting.CurrentMilianModifyScaleLabel(currentMilianModifyScale), 0.8f, TextAnchor.MiddleLeft, null, MilianModifySetting.CurrentMilianModifyScaleDesc(currentMilianModifyScale)))
		{
			List<FloatMenuOption> list2 = new List<FloatMenuOption>();
			foreach (MilianModificationChanceScale scale2 in Enum.GetValues(typeof(MilianModificationChanceScale)))
			{
				MilianModifySetting.MilianModifyChanceScaleInfo(scale2, out var label2, out desc);
				list2.Add(new FloatMenuOption(label2, delegate
				{
					CurrentMilianModifySettingTo(scale2);
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list2));
		}
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDifficulty_Promotion_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_Promotion, "MiliraRace_ModSetting_MilianDifficulty_Promotion_Desc".Translate());
		if (MiliraRace_ModSetting_MilianDifficulty_Promotion)
		{
			listing_Standard.CheckboxLabeled("·         " + "MiliraRace_ModSetting_MilianDifficulty_FastPromotion_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_FastPromotion, "MiliraRace_ModSetting_MilianDifficulty_FastPromotion_Desc".Translate());
			listing_Standard.CheckboxLabeled("·         " + "MiliraRace_ModSetting_MilianDifficulty_WidePromotion_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_WidePromotion, "MiliraRace_ModSetting_MilianDifficulty_WidePromotion_Desc".Translate());
		}
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDifficulty_ClusterResonator_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_ClusterResonator, "MiliraRace_ModSetting_MilianDifficulty_ClusterResonator_Desc".Translate());
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDifficulty_ClusterFortress_Label".Translate(), ref MiliraRace_ModSetting_MilianDifficulty_ClusterFortress, "MiliraRace_ModSetting_MilianDifficulty_ClusterFortress_Desc".Translate());
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MiliraDifficulty_TirelessFly_Label".Translate(), ref MiliraRace_ModSetting_MiliraDifficulty_TirelessFly, "MiliraRace_ModSetting_MiliraDifficulty_TirelessFly_Desc".Translate());
		Text.Font = GameFont.Medium;
		listing_Standard.Label("Milira.ModSetting_Milira".Translate());
		Text.Font = GameFont.Small;
		listing_Standard.GapLine();
		listing_Standard.CheckboxLabeled("Milira.RaceRestrictedApparel_Label".Translate(), ref MiliraRace_ModSetting_RaceRestrictedApparel, "Milira.RaceRestrictedApparel_Desc".Translate());
		Text.Font = GameFont.Medium;
		listing_Standard.Label("Milira.ModSetting_Milian".Translate());
		Text.Font = GameFont.Small;
		listing_Standard.GapLine();
		if (listing_Standard.ButtonTextLabeledPct("Milira.MilianTabEnable_Label".Translate() + ": ", SettingUtility.TabInfoLabel(TabAvailable_MilianConfig), 0.8f, TextAnchor.MiddleLeft, null, "Milira.MilianTabEnable_Desc".Translate()))
		{
			Find.WindowStack.Add(new FloatMenu(SettingUtility.TabSettingMenu((Action<TabAvailable>)delegate(TabAvailable tab)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				TabAvailable_MilianConfig = tab;
			})));
		}
		if (listing_Standard.ButtonTextLabeledPct("Milira.MilianWorkTabEnable_Label".Translate() + ": ", SettingUtility.TabInfoLabel(TabAvailable_MilianWork), 0.8f, TextAnchor.MiddleLeft, null, "Milira.MilianWorkTabEnable_Desc".Translate()))
		{
			Find.WindowStack.Add(new FloatMenu(SettingUtility.TabSettingMenu((Action<TabAvailable>)delegate(TabAvailable tab)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				TabAvailable_MilianWork = tab;
			})));
		}
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianHairColor_Label".Translate(), ref MiliraRace_ModSetting_MilianHairColor, "MiliraRace_ModSetting_MilianHairColor_Desc".Translate());
		if (MiliraRace_ModSetting_MilianHairColor)
		{
			listing_Standard.CheckboxLabeled("·         " + "MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride_Label".Translate(), ref MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride, "MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride_Desc".Translate());
			listing_Standard.CheckboxLabeled("·         " + "MiliraRace_ModSetting_MilianHairColorOffset_Label".Translate(), ref MiliraRace_ModSetting_MilianHairColorOffset, "MiliraRace_ModSetting_MilianHairColorOffset_Desc".Translate());
		}
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDrawHeadgear_Label".Translate(), ref MiliraRace_ModSetting_MilianDrawHeadgear, "MiliraRace_ModSetting_MilianDrawHeadgear_Desc".Translate());
		listing_Standard.CheckboxLabeled("MiliraRace_ModSetting_MilianDisableWorksGestate_Label".Translate(), ref MiliraRace_ModSetting_MilianDisableWorksGestate, "MiliraRace_ModSetting_MilianDisableWorksGestate_Desc".Translate());
		listing_Standard.Gap(60f);
		if (listing_Standard.ButtonText("Ancot.DefaultSetting".Translate()))
		{
			RestoreDefaultSettings();
		}
		listing_Standard.End();
		Widgets.EndScrollView();
	}

	public static void DifficultySettingTo(MiliraDifficultyScale scale)
	{
		currentGameDifficulty = scale;
	}

	public static void CurrentMilianModifySettingTo(MilianModificationChanceScale scale)
	{
		currentMilianModifyScale = scale;
	}

	public static string CurrentDifficultyScaleLabel(MiliraDifficultyScale scale)
	{
		DifficultyScaleInfo(scale, out var label, out var _);
		return label;
	}

	public static string CurrentDifficultyScaleDesc(MiliraDifficultyScale scale)
	{
		DifficultyScaleInfo(scale, out var _, out var desc);
		return desc;
	}

	public static float DifficultyScale(MiliraDifficultyScale scale)
	{
		return scale switch
		{
			MiliraDifficultyScale.Relax => 0.2f, 
			MiliraDifficultyScale.Easy => 0.6f, 
			MiliraDifficultyScale.Normal => 1f, 
			MiliraDifficultyScale.Hard => 1.2f, 
			MiliraDifficultyScale.Crazy => 1.5f, 
			_ => 1f, 
		};
	}

	public static void DifficultyScaleInfo(MiliraDifficultyScale scale, out string label, out string desc)
	{
		label = "";
		desc = "";
		switch (scale)
		{
		case MiliraDifficultyScale.Relax:
			label = "Milira.Difficulty_Relax".Translate();
			desc = "Milira.Difficulty_RelaxDesc".Translate();
			break;
		case MiliraDifficultyScale.Easy:
			label = "Milira.Difficulty_Easy".Translate();
			desc = "Milira.Difficulty_EasyDesc".Translate();
			break;
		case MiliraDifficultyScale.Normal:
			label = "Milira.Difficulty_Normal".Translate();
			desc = "Milira.Difficulty_NormalDesc".Translate();
			break;
		case MiliraDifficultyScale.Hard:
			label = "Milira.Difficulty_Hard".Translate();
			desc = "Milira.Difficulty_HardDesc".Translate();
			break;
		case MiliraDifficultyScale.Crazy:
			label = "Milira.Difficulty_Crazy".Translate();
			desc = "Milira.Difficulty_CrazyDesc".Translate();
			break;
		}
	}

	public static void RestoreDefaultSettings()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		MiliraRace_ModSetting_StoryOverall = true;
		MiliraRace_ModSetting_MilianClusterInMap = true;
		MiliraRace_ModSetting_MilianSmallClusterInMap = true;
		MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality = false;
		MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial = false;
		MiliraRace_ModSetting_MilianDifficulty_Promotion = true;
		MiliraRace_ModSetting_MilianDifficulty_FastPromotion = false;
		MiliraRace_ModSetting_MilianDifficulty_WidePromotion = false;
		MiliraRace_ModSetting_MilianDifficulty_ClusterResonator = true;
		MiliraRace_ModSetting_MilianDifficulty_ClusterFortress = false;
		MiliraRace_ModSetting_MiliraDifficulty_TirelessFly = false;
		MiliraRace_ModSetting_RaceRestrictedApparel = false;
		MiliraRace_ModSetting_MilianConfigTab = true;
		MiliraRace_ModSetting_MilianHairColor = false;
		MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride = true;
		MiliraRace_ModSetting_MilianHairColorOffset = false;
		MiliraRace_ModSetting_MilianDrawHeadgear = true;
		MiliraRace_ModSetting_MilianDisableWorksGestate = false;
		currentGameDifficulty = MiliraDifficultyScale.Easy;
		currentMilianModifyScale = MilianModificationChanceScale.Normal;
		TabAvailable_MilianConfig = (TabAvailable)2;
		TabAvailable_MilianWork = (TabAvailable)0;
	}
}
