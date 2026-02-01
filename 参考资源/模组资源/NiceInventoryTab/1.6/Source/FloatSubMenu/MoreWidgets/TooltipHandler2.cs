using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace MoreWidgets;

public static class TooltipHandler2
{
	private static readonly Dictionary<int, ActiveExpandedTip> activeTips = new Dictionary<int, ActiveExpandedTip>();

	private static int frame = 0;

	private static readonly List<int> dyingTips = new List<int>(32);

	private const float SpaceBetweenTooltips = 2f;

	private static readonly List<ActiveExpandedTip> drawingTips = new List<ActiveExpandedTip>();

	public static void ClearTooltipsFrom(Rect rect)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type != 7 || !Mouse.IsOver(rect))
		{
			return;
		}
		dyingTips.Clear();
		foreach (KeyValuePair<int, ActiveExpandedTip> activeTip in activeTips)
		{
			if (activeTip.Value.lastTriggerFrame == frame)
			{
				dyingTips.Add(activeTip.Key);
			}
		}
		for (int i = 0; i < dyingTips.Count; i++)
		{
			activeTips.Remove(dyingTips[i]);
		}
	}

	public static void TipRegion(Rect rect, Action<Rect> draw, Func<Vector2> size, int uniqueId)
	{
		TipRegion(rect, new ExpandedTip(draw, size, uniqueId));
	}

	public static void TipRegion(Rect rect, ExpandedTip tip)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 7 && (Mouse.IsOver(rect) || DebugViewSettings.drawTooltipEdges) && (tip.size != null || tip.draw != null) && !SteamDeck.KeyboardShowing)
		{
			if (DebugViewSettings.drawTooltipEdges)
			{
				Widgets.DrawBox(rect);
			}
			if (!activeTips.ContainsKey(tip.uniqueId))
			{
				ActiveExpandedTip value = new ActiveExpandedTip(tip);
				activeTips.Add(tip.uniqueId, value);
				activeTips[tip.uniqueId].firstTriggerTime = Time.realtimeSinceStartup;
			}
			activeTips[tip.uniqueId].lastTriggerFrame = frame;
		}
	}

	public static void DoTooltipGUI()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		if (!CellInspectorDrawer.active)
		{
			DrawActiveTips();
			if ((int)Event.current.type == 7)
			{
				CleanActiveTooltips();
				frame++;
			}
		}
	}

	private static void DrawActiveTips()
	{
		if (activeTips.Count == 0)
		{
			return;
		}
		drawingTips.Clear();
		foreach (ActiveExpandedTip value in activeTips.Values)
		{
			if ((double)Time.realtimeSinceStartup > value.firstTriggerTime + (double)value.tip.delay)
			{
				drawingTips.Add(value);
			}
		}
		if (drawingTips.Any())
		{
			drawingTips.SortStable(CompareTooltipsByPriority);
			Vector2 pos = CalculateInitialTipPosition(drawingTips);
			for (int i = 0; i < drawingTips.Count; i++)
			{
				pos.y += drawingTips[i].DrawTooltip(pos);
				pos.y += 2f;
			}
			drawingTips.Clear();
		}
	}

	private static void CleanActiveTooltips()
	{
		dyingTips.Clear();
		foreach (KeyValuePair<int, ActiveExpandedTip> activeTip in activeTips)
		{
			if (activeTip.Value.lastTriggerFrame != frame)
			{
				dyingTips.Add(activeTip.Key);
			}
		}
		for (int i = 0; i < dyingTips.Count; i++)
		{
			activeTips.Remove(dyingTips[i]);
		}
	}

	private static Vector2 CalculateInitialTipPosition(List<ActiveExpandedTip> drawingTips)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < drawingTips.Count; i++)
		{
			Rect tipRect = drawingTips[i].TipRect;
			num += tipRect.height;
			num2 = Mathf.Max(num2, tipRect.width);
			if (i != drawingTips.Count - 1)
			{
				num += 2f;
			}
		}
		return GenUI.GetMouseAttachedWindowPos(num2, num);
	}

	private static int CompareTooltipsByPriority(ActiveExpandedTip A, ActiveExpandedTip B)
	{
		int num = 0 - A.tip.priority;
		int value = 0 - B.tip.priority;
		return num.CompareTo(value);
	}
}
