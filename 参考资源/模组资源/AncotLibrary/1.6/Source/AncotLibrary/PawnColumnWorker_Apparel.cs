using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_Apparel : PawnColumnWorker
{
	protected virtual int Width => def.width;

	protected virtual int Padding => 2;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		List<Apparel> apparelsFor = GetApparelsFor(pawn);
		if (apparelsFor.NullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < apparelsFor.Count; i++)
		{
			Apparel apparel = apparelsFor[i];
			Vector2 iconSize = GetIconSize(pawn);
			Rect rect2 = new Rect(rect.x + rect.height * (float)i, rect.y, iconSize.x, iconSize.y);
			GUI.color = GetIconColor(apparel);
			GUI.DrawTexture(rect2, (Texture)apparel.def.uiIcon);
			GUI.color = AncotUtility.GetQualityColor(apparel);
			Rect rect3 = new Rect(rect2.x, rect2.y, 7f, 7f);
			GUI.DrawTexture(rect3, (Texture)AncotLibraryIcon.SmallPoint);
			GUI.color = Color.white;
			if (Mouse.IsOver(rect2))
			{
				Widgets.DrawHighlightIfMouseover(rect2);
				TooltipHandler.TipRegion(rect2, GetIconTip(apparel));
			}
		}
	}

	public override int GetMinWidth(PawnTable table)
	{
		int num = 1;
		foreach (Pawn item in table.PawnsListForReading)
		{
			int valueOrDefault = (item.apparel?.WornApparel?.Count).GetValueOrDefault();
			if (valueOrDefault > num)
			{
				num = valueOrDefault;
			}
		}
		float x = GetIconSize(null).x;
		float num2 = 4f;
		return Mathf.CeilToInt((float)num * x + num2);
	}

	public override int GetMaxWidth(PawnTable table)
	{
		return GetMinWidth(table);
	}

	public override int GetMinCellHeight(Pawn pawn)
	{
		return Mathf.Max(base.GetMinCellHeight(pawn), Mathf.CeilToInt(GetIconSize(pawn).y));
	}

	protected List<Apparel> GetApparelsFor(Pawn pawn)
	{
		return pawn.apparel?.WornApparel;
	}

	protected List<Texture2D> GetIconsFor(Pawn pawn)
	{
		List<Texture2D> list = new List<Texture2D>();
		List<Apparel> list2 = pawn.apparel?.WornApparel;
		if (list2.NullOrEmpty())
		{
			return null;
		}
		foreach (Apparel item in list2)
		{
			list.Add(item.def.uiIcon);
		}
		return list;
	}

	protected virtual string GetIconTip(Apparel ap)
	{
		if (ap == null)
		{
			return "";
		}
		return ap.LabelCap;
	}

	protected virtual Color GetIconColor(Apparel ap)
	{
		if (ap.def.MadeFromStuff)
		{
			return ap.Stuff.stuffProps.color;
		}
		return Color.white;
	}

	protected virtual void ClickedIcon(Apparel ap)
	{
	}

	protected virtual Vector2 GetIconSize(Pawn pawn)
	{
		return new Vector2(30f, 30f);
	}
}
