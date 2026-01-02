using UnityEngine;
using Verse;

namespace AncotLibrary;

public class AncotLibrarySettings : ModSettings
{
	public static readonly double version = 1.43;

	private static Vector2 scrollPosition = Vector2.zero;

	public static Color color_Awful = new Color(0.66f, 0.22f, 0.22f, 1f);

	public static Color color_Poor = new Color(0.58f, 0.44f, 0.34f, 1f);

	public static Color color_Normal = new Color(0.75f, 0.75f, 0.75f, 1f);

	public static Color color_Good = new Color(0.71f, 0.75f, 0.32f, 1f);

	public static Color color_Excellent = new Color(0.37f, 0.75f, 0.33f, 1f);

	public static Color color_Masterwork = new Color(0f, 1f, 1f, 1f);

	public static Color color_Legendary = new Color(0.69f, 0.32f, 0.79f, 0.88f);

	public static bool turretSystem_AimingIndicator = true;

	public static bool turretSystem_AimingIndicator_NonPlayer = false;

	public static bool turretSystem_IndicatorOnlyDrawSelected = true;

	public static bool turretSystem_IndicatorOnlyDrawSelected_NonPlayer = false;

	public static bool turretSystem_IndicatorOnlyDrawForced = true;

	public static Color turretSystem_IndicatorColor_Player = new Color(1f, 0.7f, 0.7f);

	public static Color turretSystem_IndicatorColor_NonPlayer = new Color(1f, 0.5f, 0.5f);

	public static bool weaponExtraRender_Draw = true;

	public static bool apparelPolicy_GenerateAtStart = true;

	public static bool apparelPolicy_AutoSetForColonist = true;

	public static TabAvailable drone_TabAvailable = TabAvailable.ButtonAndMenu;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref color_Awful, "color_Awful", new Color(0.66f, 0.22f, 0.22f, 1f), forceSave: true);
		Scribe_Values.Look(ref color_Poor, "color_Poor", new Color(0.58f, 0.44f, 0.34f, 1f), forceSave: true);
		Scribe_Values.Look(ref color_Normal, "color_Normal", new Color(0.75f, 0.75f, 0.75f, 1f), forceSave: true);
		Scribe_Values.Look(ref color_Good, "color_Good", new Color(0.71f, 0.75f, 0.32f, 1f), forceSave: true);
		Scribe_Values.Look(ref color_Excellent, "color_Excellent", new Color(0.37f, 0.75f, 0.33f, 1f), forceSave: true);
		Scribe_Values.Look(ref color_Masterwork, "color_Masterwork", new Color(0f, 1f, 1f, 1f), forceSave: true);
		Scribe_Values.Look(ref color_Legendary, "color_Legendary", new Color(0.69f, 0.32f, 0.79f, 0.88f), forceSave: true);
		Scribe_Values.Look(ref turretSystem_AimingIndicator, "turretSystem_AimingIndicator", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref turretSystem_IndicatorOnlyDrawSelected, "turretSystem_IndicatorOnlyDrawSelected", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref turretSystem_IndicatorOnlyDrawSelected_NonPlayer, "turretSystem_IndicatorOnlyDrawSelected_NonPlayer", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref turretSystem_IndicatorOnlyDrawForced, "turretSystem_IndicatorOnlyDrawForced", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref turretSystem_AimingIndicator_NonPlayer, "turretSystem_AimingIndicator_NonPlayer", defaultValue: false, forceSave: true);
		Scribe_Values.Look(ref turretSystem_IndicatorColor_Player, "turretSystem_IndicatorColor_Player", new Color(1f, 0.5f, 0.5f), forceSave: true);
		Scribe_Values.Look(ref turretSystem_IndicatorColor_NonPlayer, "turretSystem_IndicatorColor_NonPlayer", new Color(1f, 0.5f, 0.5f), forceSave: true);
		Scribe_Values.Look(ref weaponExtraRender_Draw, "weaponExtraRender_Draw", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref apparelPolicy_GenerateAtStart, "apparelPolicy_GenerateAtStart", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref apparelPolicy_AutoSetForColonist, "apparelPolicy_AutoSetForColonist", defaultValue: true, forceSave: true);
		Scribe_Values.Look(ref drone_TabAvailable, "drone_TabAvailable", TabAvailable.ButtonAndMenu, forceSave: true);
	}

	public static void DoWindowContents(Rect rect)
	{
		float num = 1100f;
		if (turretSystem_AimingIndicator_NonPlayer)
		{
			num += 50f;
		}
		Rect rect2 = new Rect(rect.x, rect.y, rect.width - 20f, num);
		Widgets.BeginScrollView(rect, ref scrollPosition, rect2);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect2);
		listing_Standard.Gap(5f);
		QualityColorSettings(listing_Standard);
		listing_Standard.GapLine();
		TurretSystemSettings(listing_Standard);
		listing_Standard.GapLine();
		WeaponExtraRenderSettings(listing_Standard);
		listing_Standard.GapLine();
		ApparelPolicySettings(listing_Standard);
		listing_Standard.GapLine();
		DroneSettings(listing_Standard);
		listing_Standard.GapLine();
		listing_Standard.End();
		Widgets.EndScrollView();
	}

	public static void DoHeadLine(string label, string desc, Listing_Standard listing_Standard)
	{
		Text.Font = GameFont.Medium;
		listing_Standard.Label(label);
		Text.Font = GameFont.Small;
		listing_Standard.Label(desc);
		listing_Standard.GapLine();
	}

	public static void QualityColorSettings(Listing_Standard listing_Standard)
	{
		DoHeadLine("Ancot.QualityColorSetting".Translate(), "Ancot.QualityColorSettingDesc".Translate(), listing_Standard);
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Awful".Translate() + ": ").Colorize(color_Awful), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Awful, delegate(Color newColor)
			{
				color_Awful = newColor;
			}));
		}
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Poor".Translate() + ": ").Colorize(color_Poor), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Poor, delegate(Color newColor)
			{
				color_Poor = newColor;
			}));
		}
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Normal".Translate() + ": ").Colorize(color_Normal), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Normal, delegate(Color newColor)
			{
				color_Normal = newColor;
			}));
		}
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Good".Translate() + ": ").Colorize(color_Good), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Good, delegate(Color newColor)
			{
				color_Good = newColor;
			}));
		}
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Excellent".Translate() + ": ").Colorize(color_Excellent), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Excellent, delegate(Color newColor)
			{
				color_Excellent = newColor;
			}));
		}
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Masterwork".Translate() + ": ").Colorize(color_Masterwork), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Masterwork, delegate(Color newColor)
			{
				color_Masterwork = newColor;
			}));
		}
		if (listing_Standard.ButtonTextLabeledPct(("QualityCategory_Legendary".Translate() + ": ").Colorize(color_Legendary), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(color_Legendary, delegate(Color newColor)
			{
				color_Legendary = newColor;
			}));
		}
		if (listing_Standard.ButtonText("Ancot.DefaultSetting".Translate() + "-" + "Ancot.QualityColorSetting".Translate()))
		{
			RestoreDefaultSetting_QualityColor();
		}
	}

	public static void RestoreDefaultSetting_QualityColor()
	{
		color_Awful = new Color(0.66f, 0.22f, 0.22f, 1f);
		color_Poor = new Color(0.58f, 0.44f, 0.34f, 1f);
		color_Normal = new Color(0.75f, 0.75f, 0.75f, 1f);
		color_Good = new Color(0.71f, 0.75f, 0.32f, 1f);
		color_Excellent = new Color(0.37f, 0.75f, 0.33f, 1f);
		color_Masterwork = new Color(0f, 1f, 1f, 1f);
		color_Legendary = new Color(0.69f, 0.32f, 0.79f, 0.88f);
	}

	public static void TurretSystemSettings(Listing_Standard listing_Standard)
	{
		DoHeadLine("Ancot.TurretSystemSetting".Translate(), "Ancot.TurretSystemSettingDesc".Translate(), listing_Standard);
		listing_Standard.CheckboxLabeled("Ancot.TurretSystem_Indicator".Translate(), ref turretSystem_AimingIndicator, "Ancot.TurretSystem_IndicatorDesc".Translate());
		if (turretSystem_AimingIndicator)
		{
			if (listing_Standard.ButtonTextLabeledPct(("   -" + "Ancot.TurretSystem_IndicatorColor_Player".Translate() + ": ").Colorize(turretSystem_IndicatorColor_Player), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
			{
				Find.WindowStack.Add(new Dialog_ColorPicker(turretSystem_IndicatorColor_Player, delegate(Color newColor)
				{
					turretSystem_IndicatorColor_Player = newColor;
				}));
			}
			listing_Standard.CheckboxLabeled("   -" + "Ancot.TurretSystem_IndicatorOnlyDrawSelected".Translate(), ref turretSystem_IndicatorOnlyDrawSelected, "Ancot.TurretSystem_IndicatorOnlyDrawSelectedDesc".Translate());
			listing_Standard.CheckboxLabeled("   -" + "Ancot.TurretSystem_IndicatorOnlyDrawForced".Translate(), ref turretSystem_IndicatorOnlyDrawForced, "Ancot.TurretSystem_IndicatorOnlyDrawForcedDesc".Translate());
			listing_Standard.CheckboxLabeled("   -" + "Ancot.TurretSystem_Indicator_NonPlayer".Translate(), ref turretSystem_AimingIndicator_NonPlayer, "Ancot.TurretSystem_Indicator_NonPlayerDesc".Translate());
			if (turretSystem_AimingIndicator_NonPlayer)
			{
				listing_Standard.CheckboxLabeled("      -" + "Ancot.TurretSystem_IndicatorOnlyDrawSelected_NonPlayer".Translate(), ref turretSystem_IndicatorOnlyDrawSelected_NonPlayer, "Ancot.TurretSystem_IndicatorOnlyDrawSelected_NonPlayerDesc".Translate());
				if (listing_Standard.ButtonTextLabeledPct(("      -" + "Ancot.TurretSystem_IndicatorColor_NonPlayer".Translate() + ": ").Colorize(turretSystem_IndicatorColor_NonPlayer), "Ancot.Change".Translate(), 0.8f, TextAnchor.MiddleLeft))
				{
					Find.WindowStack.Add(new Dialog_ColorPicker(turretSystem_IndicatorColor_NonPlayer, delegate(Color newColor)
					{
						turretSystem_IndicatorColor_NonPlayer = newColor;
					}));
				}
			}
		}
		if (listing_Standard.ButtonText("Ancot.DefaultSetting".Translate() + "-" + "Ancot.TurretSystemSetting".Translate()))
		{
			RestoreDefaultSetting_TurretSystem();
		}
	}

	public static void RestoreDefaultSetting_TurretSystem()
	{
		turretSystem_AimingIndicator = true;
		turretSystem_AimingIndicator_NonPlayer = false;
		turretSystem_IndicatorOnlyDrawSelected = true;
		turretSystem_IndicatorOnlyDrawSelected_NonPlayer = false;
		turretSystem_IndicatorOnlyDrawForced = true;
		turretSystem_IndicatorColor_Player = new Color(1f, 0.7f, 0.7f);
		turretSystem_IndicatorColor_NonPlayer = new Color(1f, 0.5f, 0.5f);
	}

	public static void WeaponExtraRenderSettings(Listing_Standard listing_Standard)
	{
		DoHeadLine("Ancot.WeaponExtraRenderSetting".Translate(), "Ancot.WeaponExtraRenderSettingDesc".Translate(), listing_Standard);
		listing_Standard.CheckboxLabeled("Ancot.WeaponExtraRender_Draw".Translate(), ref weaponExtraRender_Draw, "Ancot.WeaponExtraRender_DrawDesc".Translate());
		if (listing_Standard.ButtonText("Ancot.DefaultSetting".Translate() + "-" + "Ancot.WeaponExtraRenderSetting".Translate()))
		{
			RestoreDefaultSetting_WeaponExtraRender();
		}
	}

	public static void RestoreDefaultSetting_WeaponExtraRender()
	{
		weaponExtraRender_Draw = true;
	}

	public static void ApparelPolicySettings(Listing_Standard listing_Standard)
	{
		DoHeadLine("Ancot.ApparelPolicySettings".Translate(), "Ancot.ApparelPolicySettingsDesc".Translate(), listing_Standard);
		listing_Standard.CheckboxLabeled("Ancot.ApparelPolicy_GenerateAtStart".Translate(), ref apparelPolicy_GenerateAtStart, "Ancot.ApparelPolicy_GenerateAtStartDesc".Translate());
		listing_Standard.CheckboxLabeled("Ancot.ApparelPolicy_AutoSetForColonist".Translate(), ref apparelPolicy_AutoSetForColonist, "Ancot.ApparelPolicy_AutoSetForColonistDesc".Translate());
		if (listing_Standard.ButtonText("Ancot.ApparelPolicy_Generate".Translate()))
		{
			Find.WindowStack.Add(new FloatMenu(ApparelPolicyGenerator.ApparelPolicyMenu()));
		}
		if (listing_Standard.ButtonText("Ancot.DefaultSetting".Translate() + "-" + "Ancot.ApparelPolicySettings".Translate()))
		{
			RestoreDefaultSetting_ApparelPolicy();
		}
	}

	public static void RestoreDefaultSetting_ApparelPolicy()
	{
		apparelPolicy_GenerateAtStart = true;
		apparelPolicy_AutoSetForColonist = true;
	}

	public static void DroneSettings(Listing_Standard listing_Standard)
	{
		DoHeadLine("Ancot.DroneSettings".Translate(), "Ancot.DroneSettingsDesc".Translate(), listing_Standard);
		if (listing_Standard.ButtonTextLabeledPct("Ancot.Drone_TabAvailable".Translate() + ": ", SettingUtility.TabInfoLabel(drone_TabAvailable), 0.8f, TextAnchor.MiddleLeft, null, "Ancot.Drone_TabAvailableDesc".Translate()))
		{
			Find.WindowStack.Add(new FloatMenu(SettingUtility.TabSettingMenu(delegate(TabAvailable tab)
			{
				drone_TabAvailable = tab;
			})));
		}
		if (listing_Standard.ButtonText("Ancot.DefaultSetting".Translate() + "-" + "Ancot.DroneSettings".Translate()))
		{
			RestoreDefaultSetting_DroneSettings();
		}
	}

	public static void RestoreDefaultSetting_DroneSettings()
	{
		drone_TabAvailable = TabAvailable.ButtonAndMenu;
	}
}
