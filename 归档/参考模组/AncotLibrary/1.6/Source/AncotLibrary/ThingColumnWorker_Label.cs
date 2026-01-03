using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ThingColumnWorker_Label : ThingColumnWorker
{
	private const int LeftMargin = 3;

	private const float PortraitCameraZoom = 1.2f;

	private static Dictionary<string, TaggedString> labelCache = new Dictionary<string, TaggedString>();

	private static float labelCacheForWidth = -1f;

	protected virtual TextAnchor LabelAlignment => TextAnchor.MiddleLeft;

	protected override TextAnchor DefaultHeaderAlignment => TextAnchor.LowerLeft;

	protected override float GetHeaderOffsetX(Rect rect)
	{
		return 33f;
	}

	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
		Rect rect2 = new Rect(rect.x, rect.y, rect.width, Mathf.Min(rect.height, def.groupable ? rect.height : ((float)GetMinCellHeight(thing))));
		Rect rect3 = rect2;
		rect3.xMin += 3f;
		if (def.showIcon)
		{
			rect3.xMin += rect2.height;
			Rect rect4 = new Rect(rect2.x, rect2.y, rect2.height, rect2.height);
			if (Find.Selector.IsSelected(thing))
			{
				SelectionDrawerUtility.DrawSelectionOverlayWholeGUI(rect4.ContractedBy(2f));
			}
			Widgets.ThingIcon(rect4, thing);
		}
		if (Mouse.IsOver(rect2))
		{
			GUI.DrawTexture(rect2, (Texture)TexUI.HighlightTex);
		}
		TaggedString taggedString = thing.LabelShortCap;
		if (rect3.width != labelCacheForWidth)
		{
			labelCacheForWidth = rect3.width;
			labelCache.Clear();
		}
		if (Text.CalcSize(taggedString).x > rect3.width)
		{
			taggedString = taggedString.Truncate(rect3.width, labelCache);
		}
		Text.Font = GameFont.Small;
		Text.Anchor = LabelAlignment;
		Text.WordWrap = false;
		Widgets.Label(rect3, taggedString);
		Text.WordWrap = true;
		Text.Anchor = TextAnchor.UpperLeft;
		Thing thing2 = thing;
		IThingHolder parentHolder = thing.ParentHolder;
		if (parentHolder != null)
		{
			if (parentHolder.ParentHolder is Pawn { Corpse: var corpse } pawn)
			{
				thing2 = ((corpse == null) ? ((Thing)pawn) : ((Thing)corpse));
			}
			else if (parentHolder is Thing thing3)
			{
				thing2 = thing3;
			}
		}
		if (Widgets.ButtonInvisible(rect2))
		{
			CameraJumper.TryJumpAndSelect(thing2);
			if (Current.ProgramState == ProgramState.Playing && Event.current.button == 0)
			{
				Find.MainTabsRoot.EscapeCurrentTab(playSound: false);
			}
		}
		else if (Mouse.IsOver(rect2))
		{
			TipSignal tooltip = thing.GetTooltip();
			tooltip.text = "ClickToJumpTo".Translate() + "\n\n" + tooltip.text;
			TooltipHandler.TipRegion(rect2, tooltip);
		}
	}

	public override int GetMinWidth(ThingTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), 80);
	}

	public override int GetOptimalWidth(ThingTable table)
	{
		return Mathf.Clamp(165, GetMinWidth(table), GetMaxWidth(table));
	}

	public override int Compare(Thing a, Thing b)
	{
		return string.Compare(GetValueToCompare(a), GetValueToCompare(b), StringComparison.Ordinal);
	}

	private string GetValueToCompare(Thing thing)
	{
		return thing.LabelShortCap;
	}
}
