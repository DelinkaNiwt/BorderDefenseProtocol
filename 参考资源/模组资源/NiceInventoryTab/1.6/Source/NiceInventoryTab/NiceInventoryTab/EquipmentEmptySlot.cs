using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class EquipmentEmptySlot : Widget
{
	public ApparelLayerDef apparelLayer;

	public Texture2D specialTexture;

	public string awailablePartsStr;

	public List<ApparelSlotUtility.PotentialSlot> slots;

	public List<TextureAndColor> apparelIcons;

	private Bill_ProductionWithUft BoundedBill;

	private float? workToMake;

	public EquipmentEmptySlot(ApparelLayerDef al, Texture2D specialTexture, List<BodyPartGroupDef> awailable, List<ApparelSlotUtility.PotentialSlot> slots)
	{
		apparelLayer = al;
		SetFixedHeight(34f);
		this.specialTexture = specialTexture;
		if (!awailable.NullOrEmpty())
		{
			awailablePartsStr = "";
			for (int i = 0; i < Math.Min(3, awailable.Count); i++)
			{
				awailablePartsStr = awailablePartsStr + ((i != 0) ? ", " : "") + awailable[i].LabelShortCap;
			}
			if (awailable.Count > 3)
			{
				awailablePartsStr += ", ...";
			}
		}
		this.slots = slots;
	}

	private bool UnderControl()
	{
		return CommandUtility.CanControl(ITab_Pawn_Gear_Patch.lastPawn);
	}

	public override void Draw()
	{
		Rect rect = Geometry.ContractedBy(6f);
		Widgets.DrawBoxSolid(Geometry, Assets.ColorBGD);
		if (BoundedBill != null && !BoundedBill.deleted && Settings.EXR_EnableProgressBars)
		{
			DrawProgressBar();
		}
		Rect iconRect = rect;
		iconRect.width = 57.600002f;
		DrawIcon(iconRect);
		Rect rect2 = rect;
		rect2.xMin = iconRect.xMax + 6f;
		float num = Utils.CalcWidth(apparelLayer.LabelCap);
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = Assets.ColorStat;
		Widgets.Label(rect2, apparelLayer.LabelCap);
		if (!awailablePartsStr.NullOrEmpty())
		{
			float num2 = Utils.CalcWidth(awailablePartsStr);
			float num3 = rect2.width - num - 4f;
			if (num2 > num3)
			{
				awailablePartsStr = Utils.TruncateToFit(awailablePartsStr, num3, "...");
			}
			GUI.color = Color.gray;
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(rect2, awailablePartsStr);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		if (!Mouse.IsOver(Geometry))
		{
			return;
		}
		GUI.color = Color.white;
		Widgets.DrawHighlight(Geometry.ContractedBy(2f));
		if (apparelIcons == null)
		{
			apparelIcons = slots.SelectMany((ApparelSlotUtility.PotentialSlot x) => from a in x.possibleApparel
				where ITab_Pawn_Gear_Patch.lastPawn.apparel.CanWearWithoutDroppingAnything(a)
				select new TextureAndColor(Widgets.GetIconFor(a, GenStuff.DefaultStuffFor(a)), Color.white)).ToList();
		}
		TooltipHelper.DrawIconTooltip(Geometry, apparelIcons, "NIT_AvailableApparel".Translate() + ":");
		if (Widgets.ButtonInvisible(Geometry) && UnderControl() && ITab_Pawn_Gear_Patch.lastPawn.IsColonistPlayerControlled)
		{
			ApparelSlotUtility.OpenFloatMenu(slots, apparelLayer);
		}
	}

	private void DrawProgressBar()
	{
		Rect geometry = Geometry;
		geometry.yMin = geometry.yMax - 1f;
		float pct = 0f;
		if (BoundedBill.BoundUft != null)
		{
			if (!workToMake.HasValue)
			{
				workToMake = BoundedBill.recipe.WorkAmountTotal(BoundedBill.BoundUft);
			}
			pct = 1f - Mathf.Clamp01(Mathf.Max(BoundedBill.BoundUft.workLeft, 0f) / workToMake.Value);
		}
		Widgets.DrawBoxSolid(geometry.LeftPart(pct), Assets.ICArmorHeat.Color);
	}

	private void DrawIcon(Rect iconRect)
	{
		Widgets.DrawBoxSolid(iconRect, Assets.ColorBG);
		Rect rect = Utils.RectCentered(iconRect.center, iconRect.height);
		GUI.color = Color.gray;
		GUI.DrawTexture(rect, (Texture)(specialTexture ?? Assets.ApparelSlotTex));
	}

	internal void SolveProgressBar(Pawn pawn)
	{
		if (BillFinished_Patch.TryGetExistingWearBill(pawn, apparelLayer, out var existingBill))
		{
			BoundedBill = existingBill;
		}
	}
}
