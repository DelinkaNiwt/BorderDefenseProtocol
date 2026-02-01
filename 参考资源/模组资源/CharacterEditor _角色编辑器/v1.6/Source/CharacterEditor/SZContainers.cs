using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class SZContainers
{
	internal static bool DrawElementStack<T>(Rect rect, List<T> l, bool bRemoveOnClick, Action<T> removeAction, Func<T, Def> defGetter = null)
	{
		if (l.NullOrEmpty())
		{
			return false;
		}
		try
		{
			GenUI.DrawElementStack(rect, 32f, l, delegate(Rect r, T def)
			{
				GUI.DrawTexture(r, (Texture)BaseContent.ClearTex);
				if (Mouse.IsOver(r))
				{
					Widgets.DrawHighlight(r);
					string text = def.STooltip();
					TipSignal tip = new TipSignal(text, 987654);
					TooltipHandler.TipRegion(r, tip);
				}
				Texture2D tIcon = def.GetTIcon();
				tIcon = tIcon ?? BaseContent.BadTex;
				if (Widgets.ButtonImage(r, tIcon, doMouseoverSound: false))
				{
					if (bRemoveOnClick)
					{
						removeAction(def);
						throw new Exception("removed");
					}
					if (defGetter != null)
					{
						WindowTool.Open(new Dialog_InfoCard(defGetter(def)));
					}
				}
			}, (T def) => 32f, 4f, 5f, allowOrderOptimization: false);
		}
		catch
		{
		}
		return true;
	}
}
