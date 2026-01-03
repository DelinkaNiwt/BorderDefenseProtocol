using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_Color : PawnColumnWorker
{
	protected virtual int Width => def.width;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		Vector2 iconSize = GetIconSize(pawn);
		int num = (int)((rect.width - iconSize.x) / 2f);
		int num2 = Mathf.Max((int)((30f - iconSize.y) / 2f), 0);
		Rect rect2 = new Rect(rect.x + (float)num, rect.y + (float)num2, iconSize.x, iconSize.y);
		Widgets.DrawBoxSolid(rect2, GetColor(pawn));
		if (Mouse.IsOver(rect2))
		{
			Widgets.DrawHighlightIfMouseover(rect2);
			string iconTip = GetIconTip(pawn);
			if (!iconTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect2, iconTip);
			}
		}
	}

	public override int GetMinWidth(PawnTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), Width);
	}

	public override int GetMaxWidth(PawnTable table)
	{
		return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
	}

	public override int GetMinCellHeight(Pawn pawn)
	{
		return Mathf.Max(base.GetMinCellHeight(pawn), Mathf.CeilToInt(GetIconSize(pawn).y));
	}

	protected virtual string GetIconTip(Pawn pawn)
	{
		return $"({GetColor(pawn).r:F2}, {GetColor(pawn).g:F2}, {GetColor(pawn).b:F2})";
	}

	protected virtual Color GetColor(Pawn pawn)
	{
		return Color.white;
	}

	protected virtual Vector2 GetIconSize(Pawn pawn)
	{
		return new Vector2(20f, 20f);
	}
}
