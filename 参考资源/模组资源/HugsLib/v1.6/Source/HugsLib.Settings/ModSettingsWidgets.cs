using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Core;
using UnityEngine;
using Verse;

namespace HugsLib.Settings;

/// <summary>
/// Helper methods for drawing elements and controls that appear in the <see cref="T:HugsLib.Settings.Dialog_ModSettings" /> window.
/// </summary>
public static class ModSettingsWidgets
{
	private const float HoverMenuButtonSpacing = 3f;

	private const float HoverMenuOpacityEnabled = 0.5f;

	private const float HoverMenuOpacityDisabled = 0.08f;

	private const float HoverMenuIconSize = 32f;

	private static Texture2D InfoIconTexture => HugsLibTextures.HLInfoIcon;

	private static Texture2D MenuIconNormalTexture => HugsLibTextures.HLMenuIcon;

	private static Texture2D MenuIconPlusTexture => HugsLibTextures.HLMenuIconPlus;

	public static float HoverMenuHeight => 32f;

	/// <summary>
	/// Draws a hovering menu of 2 buttons: info and menu.
	/// </summary>
	/// <param name="topRight"></param>
	/// <param name="infoTooltip">Text for the info button tooltip. Null to disable.</param>
	/// <param name="menuEnabled">When false, the menu button is semi-transparent and non-interactable</param>
	/// <param name="extraMenuOptions">When true, uses menu-with-plus-badge icon for the button</param>
	/// <returns>true if the menu button was clicked</returns>
	public static bool DrawHandleHoverMenu(Vector2 topRight, string infoTooltip, bool menuEnabled, bool extraMenuOptions)
	{
		bool result = DrawHoverMenuButton(topRight, menuEnabled, extraMenuOptions);
		bool enabled = !string.IsNullOrEmpty(infoTooltip);
		Vector2 topLeft = new Vector2(topRight.x - 32f - 3f - (float)InfoIconTexture.width, topRight.y);
		if (DoHoverMenuButton(topLeft, InfoIconTexture, enabled).hovered)
		{
			DrawImmediateTooltip(infoTooltip);
		}
		return result;
	}

	/// <summary>
	/// Draws the menu button for the hovering menu.
	/// </summary>
	/// <param name="topRight"></param>
	/// <param name="enabled">When false, the button is semi-transparent and non-interactable</param>
	/// <param name="extraMenuOptions">When true, uses menu-with-plus-badge icon for the button</param>
	/// <returns>true if the menu button was clicked</returns>
	public static bool DrawHoverMenuButton(Vector2 topRight, bool enabled, bool extraMenuOptions)
	{
		Texture2D texture2D = (extraMenuOptions ? MenuIconPlusTexture : MenuIconNormalTexture);
		Vector2 topLeft = new Vector2(topRight.x - (float)texture2D.width, topRight.y);
		return DoHoverMenuButton(topLeft, texture2D, enabled).clicked;
	}

	internal static void OpenFloatMenu(IEnumerable<FloatMenuOption> options)
	{
		Find.WindowStack.Add(new FloatMenu(options.ToList()));
	}

	internal static void OpenExtensibleContextMenu(string firstEntryLabel, Action firstEntryActivated, Action anyEntryActivated, IEnumerable<ContextMenuEntry> additionalEntries)
	{
		OpenFloatMenu(GetOptionalMenuEntry(firstEntryLabel, (Action)Delegate.Combine(firstEntryActivated, anyEntryActivated)).Concat(CreateContextMenuOptions(additionalEntries, anyEntryActivated)));
	}

	private static IEnumerable<FloatMenuOption> GetOptionalMenuEntry(string label, Action onActivated)
	{
		IEnumerable<FloatMenuOption> result;
		if (label == null)
		{
			result = Enumerable.Empty<FloatMenuOption>();
		}
		else
		{
			IEnumerable<FloatMenuOption> enumerable = new FloatMenuOption[1]
			{
				new FloatMenuOption(label, onActivated)
			};
			result = enumerable;
		}
		return result;
	}

	private static IEnumerable<FloatMenuOption> CreateContextMenuOptions(IEnumerable<ContextMenuEntry> entries, Action anyEntryActivated)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		try
		{
			entries = entries ?? Enumerable.Empty<ContextMenuEntry>();
			foreach (ContextMenuEntry entry in entries)
			{
				entry.Validate();
				list.Add(new FloatMenuOption(entry.Label, (Action)Delegate.Combine(entry.Action, anyEntryActivated))
				{
					Disabled = entry.Disabled
				});
			}
		}
		catch (Exception e)
		{
			HugsLibController.Logger.ReportException(e);
		}
		return list;
	}

	private static (bool hovered, bool clicked) DoHoverMenuButton(Vector2 topLeft, Texture texture, bool enabled)
	{
		bool item = false;
		bool item2 = false;
		Rect rect = new Rect(topLeft.x, topLeft.y, InfoIconTexture.width, InfoIconTexture.height);
		if (enabled && Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
			item = true;
		}
		Color color = GUI.color;
		float a = (enabled ? 0.5f : 0.08f);
		GUI.color = new Color(1f, 1f, 1f, a);
		GUI.DrawTexture(rect, texture);
		GUI.color = color;
		if (enabled && Widgets.ButtonInvisible(rect))
		{
			item2 = true;
		}
		return (hovered: item, clicked: item2);
	}

	private static void DrawImmediateTooltip(string tipText)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 7)
		{
			ActiveTip activeTip = new ActiveTip(tipText);
			Rect tipRect = activeTip.TipRect;
			Vector2 mouseAttachedWindowPos = GenUI.GetMouseAttachedWindowPos(tipRect.width, tipRect.height);
			mouseAttachedWindowPos = GUIPositionLocalToGlobal(mouseAttachedWindowPos);
			activeTip.DrawTooltip(mouseAttachedWindowPos);
		}
	}

	private static Vector2 GUIPositionLocalToGlobal(Vector2 localPosition)
	{
		return localPosition + (UI.MousePositionOnUIInverted - Event.current.mousePosition);
	}
}
