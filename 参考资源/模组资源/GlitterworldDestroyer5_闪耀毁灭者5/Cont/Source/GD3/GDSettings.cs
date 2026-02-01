using System;
using Verse;
using UnityEngine;

namespace GD3
{
	public class GDSettings : ModSettings
	{
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<bool>(ref GDSettings.ReinforceNotApply, "ReinforceNotApply", false, true);
			Scribe_Values.Look<bool>(ref GDSettings.AirforceNotApply, "AirforceNotApply", false, true);
			Scribe_Values.Look<bool>(ref GDSettings.DeveloperMode, "DeveloperMode", false, true);
			Scribe_Values.Look<bool>(ref GDSettings.hitArmorCanNotApply, "hitArmorCanNotApply", false, true);
			Scribe_Values.Look<bool>(ref GDSettings.pauseWhenUnstable, "pauseWhenUnstable", false, true);
			Scribe_Values.Look<int>(ref GDSettings.threatRate, "threatRate", 100, true);
			Scribe_Values.Look<int>(ref GDSettings.mechDamageRate, "mechDamageRate", 100, true);
			Scribe_Values.Look<int>(ref GDSettings.turretDamageRate, "turretDamageRate", 100, true);
			Scribe_Values.Look<int>(ref GDSettings.damageBlockRate, "damageBlockRate", 60, true);
			Scribe_Values.Look<int>(ref GDSettings.AdvancedNodeCount, "AdvancedNodeCount", 4, true);
			Scribe_Values.Look<float>(ref GDSettings.VaporizeRange, "VaporizeRange", 4.9f, true);
			Scribe_Values.Look<int>(ref GDSettings.VaporizeRangeFake, "VaporizeRangeFake", 49, true);
			Scribe_Values.Look<int>(ref GDSettings.mechhiveInterval, "mechhiveInterval", 18, true);
			Scribe_Values.Look<int>(ref GDSettings.DetectCooldown, "DetectCooldown", 5, true);
			Scribe_Values.Look<int>(ref GDSettings.TurretConsumePower, "TurretConsumePower", 500, true);
			Scribe_Values.Look<bool>(ref GDSettings.NotNeedToResearch, "NotNeedToResearch", false, true);
			//下为玩家无权限修改的数据
			Scribe_Values.Look<float>(ref GDSettings.threatFactorPostfix, "threatFactorPostfix", 1f, true);
		}
		public static void DoWindowContents(Rect rect)
		{
			Rect outRect = new Rect(rect.x, rect.y, rect.width, rect.height);
			Rect viewRect = new Rect(rect.x, rect.y, rect.width - 26f, optionsViewRectHeight);
			Rect tmpRect = new Rect(viewRect.x, viewRect.y, viewRect.width, 999999f);
			Widgets.BeginScrollView(outRect, ref optionsScrollPosition, viewRect);
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(tmpRect);

			listing_Standard.Gap(5f);
			listing_Standard.CheckboxLabeled("GD_ReinforceTitle".Translate(), ref GDSettings.ReinforceNotApply, "GD_ReinforceTip".Translate(), 0f, 1f);
			listing_Standard.Gap(5f);
			listing_Standard.CheckboxLabeled("GD_AirforceTitle".Translate(), ref GDSettings.AirforceNotApply, "GD_AirforceTip".Translate(), 0f, 1f);
			listing_Standard.Gap(12f);
			listing_Standard.Label("GD_ThreatRateTitle".Translate() + " : " + GDSettings.threatRate.ToString() + "%", -1, "GD_ThreatRateDesc".Translate());
			GDSettings.threatRate = (int)listing_Standard.Slider((float)GDSettings.threatRate, 1, 100);
			listing_Standard.Gap(5f);
			listing_Standard.Label("GD_MechDamageTitle".Translate() + " : " + GDSettings.mechDamageRate.ToString() + "%", -1, "GD_MechDamageDesc".Translate());
			GDSettings.mechDamageRate = (int)listing_Standard.Slider((float)GDSettings.mechDamageRate, 1, 100);
			listing_Standard.Gap(5f);
			listing_Standard.Label("GD_TurretDamageTitle".Translate() + " : " + GDSettings.turretDamageRate.ToString() + "%", -1, "GD_TurretDamageDesc".Translate());
			GDSettings.turretDamageRate = (int)listing_Standard.Slider((float)GDSettings.turretDamageRate, 1, 100);
			listing_Standard.GapLine();
			listing_Standard.Gap(5f);
			listing_Standard.Label("GD_MechhiveIntervalTitle".Translate() + " : " + GDSettings.mechhiveInterval.ToString(), -1, "GD_MechhiveIntervalTip".Translate());
			GDSettings.mechhiveInterval = (int)listing_Standard.Slider(GDSettings.mechhiveInterval, 1, 30);
			listing_Standard.Gap(5f);
			listing_Standard.GapLine();
			listing_Standard.Gap(12f);
			listing_Standard.Label("GD_DetectCooldownTitle".Translate() + " : " + GDSettings.DetectCooldown.ToString());
			GDSettings.DetectCooldown = (int)listing_Standard.Slider((float)GDSettings.DetectCooldown, 0, 5);
			listing_Standard.Gap(12f);
			listing_Standard.Label("GD_TurretConsumePowerTitle".Translate() + " : " + GDSettings.TurretConsumePower.ToString());
			GDSettings.TurretConsumePower = (int)listing_Standard.Slider((float)GDSettings.TurretConsumePower, 0, 500);
			listing_Standard.Gap(12f);
			listing_Standard.Label("GD_Tip".Translate());
			listing_Standard.Gap(5f);
			listing_Standard.CheckboxLabeled("GD_ReactiveArmorTitle".Translate(), ref GDSettings.hitArmorCanNotApply, null, 0f, 1f);
			listing_Standard.Gap(12f);
			listing_Standard.CheckboxLabeled("GD_PauseWhenUnstable".Translate(), ref GDSettings.pauseWhenUnstable, null, 0f, 1f);
			listing_Standard.Gap(12f);
			listing_Standard.Label("GD_AdvancedNodeTitle".Translate() + " : " + GDSettings.AdvancedNodeCount.ToString());
			GDSettings.AdvancedNodeCount = (int)listing_Standard.Slider((float)GDSettings.AdvancedNodeCount, 2.0f, 10.0f);
			listing_Standard.Gap(12f);
			listing_Standard.Label("GD_VaporizeRangeTitle".Translate() + " : " + GDSettings.VaporizeRange.ToString());
			GDSettings.VaporizeRangeFake = (int)listing_Standard.Slider((float)GDSettings.VaporizeRangeFake, 1, 99);
			GDSettings.VaporizeRange = (float)GDSettings.VaporizeRangeFake / 10;
			listing_Standard.Gap(12f);
			listing_Standard.CheckboxLabeled("GD_NotNeedToResearch".Translate(), ref GDSettings.NotNeedToResearch, null, 0f, 1f);
			listing_Standard.Gap(5f);
			listing_Standard.GapLine();
			listing_Standard.Gap(5f);
			listing_Standard.CheckboxLabeled("GD_DeveloperMode".Translate(), ref GDSettings.DeveloperMode, "GD_DeveloperModeTip".Translate(), 0f, 1f);

			optionsViewRectHeight = listing_Standard.CurHeight + 10f;
			listing_Standard.End();
			Widgets.EndScrollView();
		}

		public static bool ReinforceNotApply = false;

		public static bool AirforceNotApply = false;

		public static bool DeveloperMode = false;

		public static bool hitArmorCanNotApply = false;

		public static bool pauseWhenUnstable = false;

		public static bool NotNeedToResearch;

		public static int threatRate = 100;

		public static int mechDamageRate = 100;

		public static int turretDamageRate = 100;

		public static int damageBlockRate = 60;

		public static int mechhiveInterval = 18;

		public static int AdvancedNodeCount = 4;

		public static float VaporizeRange = 4.9f;

		public static int VaporizeRangeFake = 49;

		public static int DetectCooldown = 5;

		public static int TurretConsumePower = 500;

		public static float threatFactorPostfix = 1f;

		private static Vector2 optionsScrollPosition;

		private static float optionsViewRectHeight;
	}
}
