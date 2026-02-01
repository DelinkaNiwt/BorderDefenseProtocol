using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

internal class ShowcaseItem : EquippedItem
{
	public QualityCategory quality;

	public ThingDef thingDef;

	public ShowcaseItem()
		: base(null, null)
	{
		thingDef = DefDatabase<ThingDef>.AllDefs.FirstOrDefault((ThingDef x) => x.defName == "Gun_AssaultRifle");
	}

	public override void Draw()
	{
		Rect org = Geometry.ContractedBy(6f);
		Widgets.DrawBoxSolid(Geometry, Assets.ColorBGD);
		DrawQualityGlow2(Geometry.ContractedBy(2f));
		(Rect left, Rect right) tuple = Utils.SplitRectByRightPart(org, ExtraInfoSep.Value, 4f);
		Rect item = tuple.left;
		Rect item2 = tuple.right;
		Rect iconRect;
		Rect rect = (iconRect = item);
		iconRect.width = iconRect.height * 1.2f;
		Rect rect2 = rect;
		rect2.xMin = iconRect.xMax + 6f;
		DrawIcon2(iconRect);
		DrawLabelAndButtons2(rect2.TopPart(0.4f));
		DrawBars(rect2.BottomPart(0.54f));
		DrawWeaponInfo(item2);
		DrawStars2(Geometry);
	}

	private void DrawLabelAndButtons2(Rect rect)
	{
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = Assets.ColorStat;
		Assets.DrawCroppedText(rect, "NIT_Showcase".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
	}

	public override void DrawWeaponInfo(Rect rect)
	{
		(Rect top, Rect bottom) tuple = Utils.SplitRectVertical(rect, 0.3333f);
		Rect item = tuple.top;
		(Rect top, Rect bottom) tuple2 = Utils.SplitRectVertical(tuple.bottom, 0.5f);
		Rect item2 = tuple2.top;
		Rect item3 = tuple2.bottom;
		warmupTime = 1.2f;
		cooldownTime = 1.7f;
		stoppingPower = 0.5f;
		fireRate = 600f;
		string text = (warmupTime.HasValue ? warmupTime.Value.ToString("F1") : "--");
		DrawIconText(item, Assets.ICReload.Icon, text + "+" + cooldownTime.ToString(Assets.Format_Seconds), (Thing thing, Pawn pawn) => "");
		DrawIconText(item2, Assets.ICStop.Icon, stoppingPower.Value.ToString("F1"), (Thing thing, Pawn pawn) => "");
		DrawIconText(item3, Assets.ICFireRate.Icon, fireRate.Value.ToString(Assets.Format_FireRate), (Thing thing, Pawn pawn) => "");
	}

	private void DrawQualityGlow2(Rect rect)
	{
		float qualityGlow = Settings.GetQualityGlow(quality);
		if (qualityGlow > 0f)
		{
			GUI.color = ColorUtils.ChangeAlpha(Settings.GetQualityColor(quality, highlighted: true), Mathf.Clamp01(qualityGlow));
			GUI.DrawTexture(rect, (Texture)Assets.GlowQualityTex);
			if (qualityGlow > 1f)
			{
				GUI.color = ColorUtils.ChangeAlpha(Settings.GetQualityColor(quality, highlighted: true), Mathf.Clamp01(qualityGlow - 1f));
				GUI.DrawTexture(rect, (Texture)Assets.GlowQualityTex);
			}
		}
	}

	private void DrawStars2(Rect globalRect)
	{
		if (!Settings.ShowQualityOnlyOnHover(quality) || Mouse.IsOver(Geometry))
		{
			int num = (int)quality;
			float num2 = Settings.StarSize * 6f;
			Rect rect = new Rect(globalRect.center.x - num2 * 0.5f, globalRect.y - Settings.StarSize * 0.5f + 2f, num2, Settings.StarSize);
			for (int i = 0; i < 6; i++)
			{
				GUI.color = Settings.GetQualityColor(quality, i < num);
				GUI.DrawTexture(new Rect(rect.x + Settings.StarSize * (float)i, rect.y, Settings.StarSize, Settings.StarSize), (Texture)Assets.QualityTex);
			}
		}
	}

	public override void DrawBars(Rect rect)
	{
		DrawBar(rect.TopHalf(), "", DamageUtility.MaxDPSForWeapons * 0.6f, DamageUtility.GetMaxDPS(null), Assets.ICDamageRanged, "F1");
		var (rect2, rect3) = Utils.SplitRect(rect.BottomHalf(), 0.5f, 4f);
		DrawBar(rect2, "", 4f, 10f, Assets.ICMass, "F1", 3, affect_Color_correction: false);
		DrawBar(rect3, "", 0.2f, 0.6f, Assets.ICArmorPen, "%F1", 3);
	}

	private void DrawIcon2(Rect iconRect)
	{
		Widgets.DrawBoxSolid(iconRect, Assets.ColorBG);
		Rect rect = Utils.RectCentered(iconRect.center, iconRect.height);
		GUI.color = Color.white;
		if (thingDef != null)
		{
			GUI.DrawTexture(rect, (Texture)thingDef.uiIcon);
		}
	}
}
