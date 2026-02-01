using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class Settings : ModSettings
{
	public static float TabHeight;

	public static readonly float TabDefaultWidth;

	public static float TabWidth;

	public static bool ApparelSlotsVisible;

	public static bool DrugImpactVisible;

	public static bool DebuffVisible;

	public static bool EnableAutoWear;

	public static bool AutoRenameBillLabel;

	public static bool EXR_QualityStars;

	public static bool EXR_OptMaster;

	public static bool EXR_EnableProgressBars;

	public static bool CE_ApplyAim;

	public static bool CE_ShowAP;

	private int currentTab;

	public static bool CLI_VanillaButtons;

	public static bool WeaponsAreEquipment;

	public static float DepressionMode;

	public static Color DepressionColor;

	public static bool NoArmorCap;

	public static readonly float StarSize;

	public static bool ItemBarsWithText;

	public int SelectedQualityCategory = 3;

	private ShowcaseItem showcaseItem;

	private GroupBox ShowcaseZone;

	private StatDrawer MeleeDPSStat;

	private StatDrawer RangedDPSStat;

	private StatDrawer SocialStat;

	private StatDrawer MoveStat;

	public static List<Color> qualityColors;

	public static List<float> qualityGlow;

	public static List<bool> qualityOnlyOnHover;

	public void DoWindowContents(Rect inRect)
	{
		List<TabRecord> list = new List<TabRecord>
		{
			new TabRecord("SettingsCommonTab".Translate(), delegate
			{
				currentTab = 0;
			}, currentTab == 0),
			new TabRecord("SettingsColorsTab".Translate(), delegate
			{
				currentTab = 1;
			}, currentTab == 1),
			new TabRecord("SettingsExperimentalTab".Translate(), delegate
			{
				currentTab = 2;
			}, currentTab == 2)
		};
		if (ModIntegration.CLActive)
		{
			list.Add(new TabRecord("SettingsIntegrationTab".Translate(), delegate
			{
				currentTab = 3;
			}, currentTab == 3));
		}
		inRect = inRect.ContractedBy(8f);
		inRect.yMin += 40f;
		TabDrawer.DrawTabs(inRect, list);
		Rect rect = inRect;
		rect.yMin += 32f;
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.ColumnWidth = rect.width;
		listing_Standard.Begin(rect);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		switch (currentTab)
		{
		case 0:
			DrawCommonTab(listing_Standard, rect);
			break;
		case 1:
			DrawColorsTab(listing_Standard, rect);
			break;
		case 2:
			DrawExperimentalTab(listing_Standard, rect);
			break;
		case 3:
			DrawIntegrationTab(listing_Standard, rect);
			break;
		}
		listing_Standard.End();
	}

	private void DrawIntegrationTab(Listing_Standard listing_Standard, Rect tabContentRect)
	{
		listing_Standard.Label("Compositable Loadouts:");
		listing_Standard.CheckboxLabeled("NIT_CLI_UseVanillaButtons".Translate(), ref CLI_VanillaButtons);
	}

	private void DrawExperimentalTab(Listing_Standard listing_Standard, Rect tabContentRect)
	{
		listing_Standard.CheckboxLabeled("NIT_EnableOptionToOpenApparelMaster".Translate(), ref EXR_OptMaster);
		listing_Standard.CheckboxLabeled("NIT_EnableProgressBars".Translate(), ref EXR_EnableProgressBars);
		bool eXR_QualityStars = EXR_QualityStars;
		listing_Standard.CheckboxLabeled("NIT_EnableStarsAsQuality".Translate(), ref EXR_QualityStars);
		if (!eXR_QualityStars && EXR_QualityStars)
		{
			InitializeColors(force: false);
		}
		if (EXR_QualityStars)
		{
			listing_Standard.Gap(10f);
			Rect rect = listing_Standard.GetRect(180f);
			DrawQualityColorsSettings(rect, listing_Standard);
		}
		GUI.color = Color.white;
	}

	private void DrawQualityColorsSettings(Rect qualityColorsRect, Listing_Standard listing_Standard)
	{
		(Rect left, Rect right) tuple = Utils.SplitRect(qualityColorsRect, 0.2f, 20f);
		Rect item = tuple.left;
		Rect item2 = tuple.right;
		QualityCategory[] array = new QualityCategory[7]
		{
			QualityCategory.Awful,
			QualityCategory.Poor,
			QualityCategory.Normal,
			QualityCategory.Good,
			QualityCategory.Excellent,
			QualityCategory.Masterwork,
			QualityCategory.Legendary
		};
		float num = item.height / (float)array.Length;
		for (int i = 0; i < array.Length; i++)
		{
			QualityCategory qualityCategory = array[i];
			Rect rect = new Rect(item.x, item.y + num * (float)i, item.width, num);
			GUI.color = GetQualityColor(qualityCategory, highlighted: true);
			Widgets.Label(rect, qualityCategory.GetLabel().StripTags());
			if (SelectedQualityCategory == i)
			{
				GUI.color = Color.white;
				Widgets.DrawHighlight(rect);
			}
			else if (Widgets.ButtonInvisible(rect))
			{
				SelectedQualityCategory = i;
			}
			if (Mouse.IsOver(rect))
			{
				GUI.color = Color.white;
				Widgets.DrawHighlight(rect);
			}
		}
		GUI.color = (ModIntegration.QCActive ? Color.white : Color.gray);
		if (Widgets.ButtonText(new Rect(item.x, item.y + num * 8f, item.width, num), "NIT_MatchToQualityColorsMod".Translate(), drawBackground: true, doMouseoverSound: true, ModIntegration.QCActive))
		{
			MatchToQualityColors();
		}
		GUI.color = Color.white;
		if (Widgets.ButtonText(new Rect(item.x, item.y + num * 9f, item.width, num), "NIT_Reset".Translate()))
		{
			InitializeColors(force: true);
		}
		QualityCategory qualityCategory2 = array[SelectedQualityCategory];
		Color qualityColor = GetQualityColor(qualityCategory2, highlighted: true);
		float num2 = GetQualityGlow(qualityCategory2);
		float num3 = item2.height / 5f;
		qualityColor.r = Widgets.HorizontalSlider(new Rect(item2.x, item2.y + num3 * 0f, item2.width, num3), qualityColor.r, 0f, 1f, middleAlignment: false, "R");
		qualityColor.g = Widgets.HorizontalSlider(new Rect(item2.x, item2.y + num3 * 1f, item2.width, num3), qualityColor.g, 0f, 1f, middleAlignment: false, "G");
		qualityColor.b = Widgets.HorizontalSlider(new Rect(item2.x, item2.y + num3 * 2f, item2.width, num3), qualityColor.b, 0f, 1f, middleAlignment: false, "B");
		num2 = Widgets.HorizontalSlider(new Rect(item2.x, item2.y + num3 * 3f, item2.width, num3), num2, 0f, 2f, middleAlignment: false, "Glow " + num2.ToStringPercent());
		bool checkOn = ShowQualityOnlyOnHover(qualityCategory2);
		Widgets.CheckboxLabeled(new Rect(item2.x, item2.y + num3 * 4f, item2.width, num3), "NIT_OnlyOnHover".Translate(), ref checkOn);
		num2 = Utils.Snap(num2, 0.01f);
		SetQualityColor(qualityCategory2, qualityColor);
		SetQualityGlow(qualityCategory2, num2);
		SetQualityOnlyOnHover(qualityCategory2, checkOn);
		listing_Standard.Gap(20f);
		Rect rect2 = listing_Standard.GetRect(60f);
		if (showcaseItem == null)
		{
			float num4 = 362f;
			showcaseItem = new ShowcaseItem();
			showcaseItem.Geometry = rect2;
			showcaseItem.Geometry.xMin = rect2.center.x - num4 / 2f;
			showcaseItem.Geometry.xMax = rect2.center.x + num4 / 2f;
		}
		showcaseItem.quality = qualityCategory2;
		showcaseItem.Draw();
	}

	private void MatchToQualityColors()
	{
		QualityColorsIntegration.DoMatch();
	}

	private void DrawColorsTab(Listing_Standard listing_Standard, Rect tabContentRect)
	{
		DepressionMode = listing_Standard.SliderLabeled("NIT_DepressionMode".Translate(DepressionMode.ToStringPercent()), DepressionMode, 0f, 1f);
		Utils.StickValue(ref DepressionMode, 0f, 0.05f);
		Utils.StickValue(ref DepressionMode, 1f, 0.05f);
		DepressionMode = Utils.Snap(DepressionMode, 0.01f);
		listing_Standard.Gap(20f);
		listing_Standard.Label("NIT_DepressionColor".Translate() + ":");
		Rect rect = listing_Standard.GetRect(20f);
		rect.y -= 20f;
		rect.xMin += 200f;
		rect.xMax -= 60f;
		Widgets.DrawBoxSolidWithOutline(rect, DepressionColor, Color.black);
		DepressionColor.r = listing_Standard.SliderLabeled($"R {Math.Round(DepressionColor.r * 255f)}", DepressionColor.r, 0f, 1f);
		DepressionColor.g = listing_Standard.SliderLabeled($"G {Math.Round(DepressionColor.g * 255f)}", DepressionColor.g, 0f, 1f);
		DepressionColor.b = listing_Standard.SliderLabeled($"B {Math.Round(DepressionColor.b * 255f)}", DepressionColor.b, 0f, 1f);
		if (ShowcaseZone == null)
		{
			FloatRef tsep = new FloatRef();
			FloatRef digitSep = new FloatRef();
			ShowcaseZone = new GroupBox("NIT_Showcase".Translate(), 1f);
			MeleeDPSStat = new StatBar("NIT_Melee".Translate(), "", MeleePawnDPS_Showcase, MaxMeleeDPS_Showcase, Assets.ICDamageMelee, tsep, digitSep);
			MeleeDPSStat.UpdateValues(null);
			RangedDPSStat = new StatBar("NIT_Ranged".Translate(), "", RangedPawnDPS_Showcase, MaxMeleeDPS_Showcase, Assets.ICDamageRangedBlue, tsep, digitSep);
			RangedDPSStat.UpdateValues(null);
			SocialStat = new StatBar("NIT_Social".Translate(), "", Social_Showcase, (Pawn _) => 2f, Assets.ICSocial, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
			SocialStat.UpdateValues(null);
			MoveStat = new StatBar("NIT_Speed".Translate(), "", Move_Showcase, MobilityUtility.MaxMoveSpeed, Assets.ICMoveSpeed, tsep, digitSep).SetFormat(Assets.Format_MoveSpeed);
			MoveStat.UpdateValues(null);
			ShowcaseZone.AddChild(MeleeDPSStat);
			ShowcaseZone.AddChild(RangedDPSStat);
			ShowcaseZone.AddChild(SocialStat);
			ShowcaseZone.AddChild(MoveStat);
			ShowcaseZone.Geometry = new Rect(tabContentRect.x + 200f, tabContentRect.y + 200f, tabContentRect.width - 420f, 130f);
			ShowcaseZone.Update();
		}
		MeleeDPSStat.UpdateValues(null);
		RangedDPSStat.UpdateValues(null);
		SocialStat.UpdateValues(null);
		MoveStat.UpdateValues(null);
		ShowcaseZone.Draw();
	}

	public static Color ColorCorrect(Color org)
	{
		if (DepressionMode == 0f)
		{
			return org;
		}
		if (DepressionMode == 1f)
		{
			return DepressionColor;
		}
		return Color.Lerp(org, DepressionColor, DepressionMode);
	}

	private static float Move_Showcase(Pawn pawn, StatDrawer statBar)
	{
		(statBar as StatBar).AddDebuff(-1.2750001f, Assets.EnviromentPenaltyColor);
		(statBar as StatBar).AddDebuff(-1.7f, Assets.PenaltyColor);
		return 4.4f;
	}

	private static float Social_Showcase(Pawn pawn, StatDrawer statBar)
	{
		return 0.67f;
	}

	private static float MaxMeleeDPS_Showcase(Pawn pawn)
	{
		return DamageUtility.MaxMeleePawnDPS;
	}

	private static float MeleePawnDPS_Showcase(Pawn pawn, StatDrawer statBar)
	{
		(statBar as StatBar).AddDebuff(-0.1f * DamageUtility.MaxMeleePawnDPS, Assets.PenaltyColor);
		return DamageUtility.MaxMeleePawnDPS * 0.6f;
	}

	private static float RangedPawnDPS_Showcase(Pawn pawn, StatDrawer statBar)
	{
		(statBar as StatBar).AddBuff(0.12f * DamageUtility.MaxMeleePawnDPS, (statBar as StatBar).ColorBar);
		return DamageUtility.MaxMeleePawnDPS * 0.75f;
	}

	private void DrawCommonTab(Listing_Standard listing_Standard, Rect tabContentRect)
	{
		float tabWidth = TabWidth;
		TabWidth = listing_Standard.SliderLabeled("GearTabWidth".Translate(TabWidth), TabWidth, 560f, 960f);
		TabWidth = Mathf.Round(TabWidth);
		Utils.StickValue(ref TabWidth, TabDefaultWidth, 16f);
		if (tabWidth != TabWidth)
		{
			ITab_Pawn_Gear_Patch.UpdateSize();
		}
		bool debuffVisible = DebuffVisible;
		listing_Standard.CheckboxLabeled("StatDebuffVisible".Translate(), ref DebuffVisible);
		if (debuffVisible != DebuffVisible)
		{
			ITab_Pawn_Gear_Patch.shouldRecache = true;
		}
		listing_Standard.CheckboxLabeled("NIT_AutoRenameBillLabelSettings".Translate(), ref AutoRenameBillLabel);
		listing_Standard.CheckboxLabeled("NIT_EnableAutoWear".Translate(), ref EnableAutoWear);
		bool weaponsAreEquipment = WeaponsAreEquipment;
		listing_Standard.CheckboxLabeled("NIT_ShowWeaponsInEquipmentSection".Translate(), ref WeaponsAreEquipment);
		if (weaponsAreEquipment != WeaponsAreEquipment)
		{
			ITab_Pawn_Gear_Patch.shouldRecache = true;
		}
	}

	static Settings()
	{
		TabHeight = 550f;
		TabDefaultWidth = 760f;
		TabWidth = TabDefaultWidth;
		ApparelSlotsVisible = false;
		DrugImpactVisible = true;
		DebuffVisible = true;
		EnableAutoWear = true;
		AutoRenameBillLabel = true;
		EXR_QualityStars = false;
		EXR_OptMaster = false;
		EXR_EnableProgressBars = false;
		CE_ApplyAim = true;
		CE_ShowAP = false;
		CLI_VanillaButtons = false;
		WeaponsAreEquipment = true;
		DepressionMode = 0f;
		DepressionColor = Color.gray;
		NoArmorCap = false;
		StarSize = 12f;
		ItemBarsWithText = false;
		qualityColors = new List<Color>(7);
		qualityGlow = new List<float>(7);
		qualityOnlyOnHover = new List<bool>(7);
	}

	private static void InitializeColors(bool force)
	{
		if (qualityColors == null || qualityGlow == null || qualityOnlyOnHover == null)
		{
			qualityColors = new List<Color>(7);
			qualityGlow = new List<float>(7);
			qualityOnlyOnHover = new List<bool>(7);
			force = true;
		}
		if (qualityColors.Count != 7 || force)
		{
			qualityColors.Clear();
			qualityColors.AddRange(new Color[7]
			{
				ColorUtils.fromHEX(9062432),
				ColorUtils.fromHEX(11171643),
				ColorUtils.fromHEX(14013909),
				ColorUtils.fromHEX(16773942),
				ColorUtils.fromHEX(65334),
				ColorUtils.fromHEX(16738559),
				ColorUtils.fromHEX(16718192)
			});
		}
		if (qualityGlow.Count != 7 || force)
		{
			qualityGlow.Clear();
			qualityGlow.AddRange(new float[7] { 0f, 0f, 0f, 0.25f, 0.66f, 1f, 2f });
		}
		if (qualityOnlyOnHover.Count != 7 || force)
		{
			qualityOnlyOnHover.Clear();
			qualityOnlyOnHover.AddRange(new bool[7]);
		}
	}

	public static Color GetQualityColor(QualityCategory qc, bool highlighted)
	{
		Color color = qualityColors[(int)qc];
		if (!highlighted)
		{
			return ColorUtils.Darker(color);
		}
		return color;
	}

	public static float GetQualityGlow(QualityCategory qc)
	{
		return qualityGlow[(int)qc];
	}

	public static bool ShowQualityOnlyOnHover(QualityCategory qc)
	{
		return qualityOnlyOnHover[(int)qc];
	}

	public static void SetQualityColor(QualityCategory qc, Color color)
	{
		qualityColors[(int)qc] = color;
	}

	public static void SetQualityGlow(QualityCategory qc, float glow)
	{
		qualityGlow[(int)qc] = glow;
	}

	public static void SetQualityOnlyOnHover(QualityCategory qc, bool value)
	{
		qualityOnlyOnHover[(int)qc] = value;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref TabWidth, "TabWidth", TabDefaultWidth);
		Scribe_Values.Look(ref ApparelSlotsVisible, "ApparelSlotsVisible", defaultValue: false);
		Scribe_Values.Look(ref DrugImpactVisible, "DrugImpactVisible", defaultValue: true);
		Scribe_Values.Look(ref DebuffVisible, "DebuffVisible", defaultValue: true);
		Scribe_Values.Look(ref AutoRenameBillLabel, "AutoRenameBillLabel", defaultValue: true);
		Scribe_Values.Look(ref EnableAutoWear, "EnableAutoWear", defaultValue: true);
		Scribe_Values.Look(ref EXR_OptMaster, "EXR_OptMaster", defaultValue: false);
		Scribe_Values.Look(ref EXR_QualityStars, "EXR_QualityStars", defaultValue: false);
		Scribe_Values.Look(ref EXR_EnableProgressBars, "EXR_EnableProgressBars", defaultValue: false);
		Scribe_Values.Look(ref CE_ApplyAim, "CE_ApplyAim", defaultValue: true);
		Scribe_Values.Look(ref CE_ShowAP, "CE_ShowAP", defaultValue: false);
		Scribe_Values.Look(ref CLI_VanillaButtons, "CLI_VanillaButtons", defaultValue: false);
		Scribe_Values.Look(ref WeaponsAreEquipment, "WeaponsAreEquipment", defaultValue: true);
		Scribe_Values.Look(ref DepressionMode, "DepressionMode", 0f);
		Scribe_Values.Look(ref DepressionColor.r, "DepressionColor_R", 0.5f);
		Scribe_Values.Look(ref DepressionColor.g, "DepressionColor_G", 0.5f);
		Scribe_Values.Look(ref DepressionColor.b, "DepressionColor_B", 0.5f);
		DepressionColor.a = 1f;
		Scribe_Collections.Look(ref qualityColors, "QualityColors", LookMode.Value);
		Scribe_Collections.Look(ref qualityGlow, "QualityGlow", LookMode.Value);
		Scribe_Collections.Look(ref qualityOnlyOnHover, "QualityOnlyOnHover", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			InitializeColors(force: false);
		}
	}
}
