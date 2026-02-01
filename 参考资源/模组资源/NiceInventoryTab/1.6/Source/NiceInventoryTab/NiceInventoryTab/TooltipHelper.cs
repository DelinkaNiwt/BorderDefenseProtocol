using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public static class TooltipHelper
{
	public static void DrawIconTooltip(Rect rect, List<TextureAndColor> icons, string title)
	{
		if (!Mouse.IsOver(rect))
		{
			return;
		}
		Vector2 mousePositionOnUIInverted = UI.MousePositionOnUIInverted;
		float iconSize = 24f;
		float spacing = 2f;
		float padding = 8f;
		float titleHeight = (string.IsNullOrEmpty(title) ? 0f : (Text.LineHeight + 4f));
		int iconsPerRow = Mathf.Min(icons.Count, 8);
		int num = Mathf.CeilToInt((float)icons.Count / (float)iconsPerRow);
		float contentWidth = (float)iconsPerRow * (iconSize + spacing) - spacing;
		float contentHeight = (float)num * (iconSize + spacing) - spacing;
		float b = 0f;
		if (!string.IsNullOrEmpty(title))
		{
			Text.Font = GameFont.Small;
			b = Text.CalcSize(title).x;
		}
		float width = Mathf.Max(contentWidth, b) + padding * 2f;
		Rect winrect = new Rect(mousePositionOnUIInverted.x + 15f, mousePositionOnUIInverted.y + 15f, width, contentHeight + titleHeight + padding * 2f);
		if (winrect.xMax > (float)UI.screenWidth)
		{
			winrect.x = (float)UI.screenWidth - winrect.width;
		}
		if (winrect.yMax > (float)UI.screenHeight)
		{
			winrect.y = (float)UI.screenHeight - winrect.height;
		}
		if (winrect.x < 0f)
		{
			winrect.x = 0f;
		}
		if (winrect.y < 0f)
		{
			winrect.y = 0f;
		}
		Find.WindowStack.ImmediateWindow(81724612, winrect, WindowLayer.Super, delegate
		{
			Rect rect2 = winrect.AtZero();
			Widgets.DrawShadowAround(rect2);
			Widgets.DrawWindowBackground(rect2);
			Widgets.DrawAtlas(rect2, ActiveTip.TooltipBGAtlas);
			float num2 = padding;
			if (!string.IsNullOrEmpty(title))
			{
				Rect rect3 = new Rect(padding, num2 - 4f, rect2.width - padding * 2f, Text.LineHeight);
				Text.Font = GameFont.Small;
				Widgets.Label(rect3, title);
				num2 += titleHeight;
			}
			float x = (rect2.width - contentWidth) / 2f;
			Rect rect4 = new Rect(x, num2, contentWidth, contentHeight);
			for (int i = 0; i < icons.Count; i++)
			{
				int num3 = i / iconsPerRow;
				int num4 = i % iconsPerRow;
				Rect rect5 = new Rect(rect4.x + (float)num4 * (iconSize + spacing), rect4.y + (float)num3 * (iconSize + spacing), iconSize, iconSize);
				GUI.color = icons[i].Color;
				GUI.DrawTexture(rect5, (Texture)icons[i].Texture);
				GUI.color = Color.white;
			}
		}, doBackground: false);
	}
}
