using UnityEngine;
using Verse;

namespace MoreWidgets;

internal class ActiveExpandedTip
{
	private const float TipMargin = 4f;

	public ExpandedTip tip;

	public double firstTriggerTime;

	public int lastTriggerFrame;

	public Rect TipRect => new Rect(default(Vector2), tip.size()).ContractedBy(-4f).RoundedCeil();

	public ActiveExpandedTip(ExpandedTip tip)
	{
		this.tip = tip;
	}

	public ActiveExpandedTip(ActiveExpandedTip cloneSource)
	{
		tip = cloneSource.tip;
		firstTriggerTime = cloneSource.firstTriggerTime;
		lastTriggerFrame = cloneSource.lastTriggerFrame;
	}

	public float DrawTooltip(Vector2 pos)
	{
		Text.Font = GameFont.Small;
		Rect bgRect = TipRect;
		bgRect.position = pos;
		if (!LongEventHandler.AnyEventWhichDoesntUseStandardWindowNowOrWaiting)
		{
			Find.WindowStack.ImmediateWindow(153 * tip.uniqueId + 62346, bgRect, WindowLayer.Super, delegate
			{
				DrawInner(bgRect.AtZero());
			}, doBackground: false);
		}
		else
		{
			Widgets.DrawShadowAround(bgRect);
			Widgets.DrawWindowBackground(bgRect);
			DrawInner(bgRect);
		}
		return bgRect.height;
	}

	private void DrawInner(Rect bgRect)
	{
		Widgets.DrawAtlas(bgRect, ActiveTip.TooltipBGAtlas);
		tip.draw(bgRect.ContractedBy(4f));
	}
}
