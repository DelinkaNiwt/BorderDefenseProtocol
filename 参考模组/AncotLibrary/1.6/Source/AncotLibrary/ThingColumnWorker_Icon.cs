using UnityEngine;
using Verse;

namespace AncotLibrary;

public abstract class ThingColumnWorker_Icon : ThingColumnWorker
{
	protected virtual int Width => 26;

	protected virtual int Padding => 2;

	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
		Texture2D iconFor = GetIconFor(thing);
		if (!(iconFor != null))
		{
			return;
		}
		Vector2 iconSize = GetIconSize(thing);
		int num = (int)((rect.width - iconSize.x) / 2f);
		int num2 = Mathf.Max((int)((30f - iconSize.y) / 2f), 0);
		Rect rect2 = new Rect(rect.x + (float)num, rect.y + (float)num2, iconSize.x, iconSize.y);
		GUI.color = GetIconColor(thing);
		GUI.DrawTexture(rect2.ContractedBy(Padding), (Texture)iconFor);
		GUI.color = Color.white;
		if (Mouse.IsOver(rect2))
		{
			string iconTip = GetIconTip(thing);
			if (!iconTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect2, iconTip);
			}
		}
		if (Widgets.ButtonInvisible(rect2, doMouseoverSound: false))
		{
			ClickedIcon(thing);
		}
		if (Mouse.IsOver(rect2) && Input.GetMouseButton(0))
		{
			PaintedIcon(thing);
		}
	}

	public override int GetMinWidth(ThingTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), Width);
	}

	public override int GetMaxWidth(ThingTable table)
	{
		return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
	}

	public override int GetMinCellHeight(Thing thing)
	{
		return Mathf.Max(base.GetMinCellHeight(thing), Mathf.CeilToInt(GetIconSize(thing).y));
	}

	public override int Compare(Thing a, Thing b)
	{
		return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
	}

	private int GetValueToCompare(Thing thing)
	{
		Texture2D iconFor = GetIconFor(thing);
		if (!(iconFor != null))
		{
			return int.MinValue;
		}
		return iconFor.GetInstanceID();
	}

	protected abstract Texture2D GetIconFor(Thing thing);

	protected virtual string GetIconTip(Thing thing)
	{
		return null;
	}

	protected virtual Color GetIconColor(Thing thing)
	{
		return Color.white;
	}

	protected virtual void ClickedIcon(Thing thing)
	{
	}

	protected virtual void PaintedIcon(Thing thing)
	{
	}

	protected virtual Vector2 GetIconSize(Thing thing)
	{
		if (GetIconFor(thing) == null)
		{
			return Vector2.zero;
		}
		return new Vector2(Width, Width);
	}
}
