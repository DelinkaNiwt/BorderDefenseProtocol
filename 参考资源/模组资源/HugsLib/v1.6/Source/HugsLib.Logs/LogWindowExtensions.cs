using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib.Shell;
using HugsLib.Utils;
using LudeonTK;
using UnityEngine;
using Verse;

namespace HugsLib.Logs;

/// <summary>
/// Allows adding custom buttons to the EditWindow_Log window.
/// </summary>
[StaticConstructorOnStartup]
public static class LogWindowExtensions
{
	/// <summary>
	/// Alignment side for custom widgets.
	/// </summary>
	public enum WidgetAlignMode
	{
		Left,
		Right
	}

	/// <summary>
	/// Callback to draw log window widgets in.
	/// </summary>
	/// <param name="logWindow">The log window being dawn.</param>
	/// <param name="widgetArea">Window area for custom widgets.</param>
	/// <param name="selectedLogMessage">The currently selected log message, or null.</param>
	/// <param name="widgetRow">Draw your widget using this to automatically align it with the others.</param>
	public delegate void WidgetDrawer(Window logWindow, Rect widgetArea, LogMessage selectedLogMessage, WidgetRow widgetRow);

	private class LogWindowWidget
	{
		public readonly WidgetDrawer Drawer;

		public readonly WidgetAlignMode Alignment;

		public LogWindowWidget(WidgetDrawer drawer, WidgetAlignMode alignment)
		{
			Drawer = drawer;
			Alignment = alignment;
		}
	}

	private static readonly float widgetRowHeight = 23f;

	private static readonly float buttonRowMarginTop = 2f;

	private static readonly Color shareButtonColor = new Color(0.3f, 1f, 0.3f, 1f);

	private static readonly Color separatorLineColor = GenColor.FromHex("303030");

	private static readonly List<LogWindowWidget> widgets = new List<LogWindowWidget>();

	private static Texture2D lineTexture;

	private static FieldInfo selectedMessageField;

	internal static float ExtensionsAreaHeight => (widgets.Count > 0) ? widgetRowHeight : 0f;

	/// <summary>
	/// Adds a new drawing callback to the log window widget drawer.
	/// </summary>
	/// <param name="drawerDelegate">The delegate called each OnGUI to draw the widget.</param>
	/// <param name="align">The side of the WidgetRow this widget should be drawn into.</param>
	public static void AddLogWindowWidget(WidgetDrawer drawerDelegate, WidgetAlignMode align = WidgetAlignMode.Left)
	{
		if (drawerDelegate == null)
		{
			throw new NullReferenceException("Drawer delegate required");
		}
		widgets.Add(new LogWindowWidget(drawerDelegate, align));
	}

	internal static void PrepareReflection()
	{
		selectedMessageField = typeof(EditWindow_Log).GetField("selectedMessage", BindingFlags.Static | BindingFlags.NonPublic);
		if (selectedMessageField != null && selectedMessageField.FieldType != typeof(LogMessage))
		{
			selectedMessageField = null;
		}
		if (selectedMessageField == null)
		{
			HugsLibController.Logger.Error("Failed to reflect EditWindow_Log.selectedMessage");
		}
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			lineTexture = SolidColorMaterials.NewSolidColorTexture(separatorLineColor);
		});
		AddOwnWidgets();
	}

	internal static void DrawLogWindowExtensions(Window window, Rect inRect)
	{
		if (widgets.Count == 0)
		{
			return;
		}
		LogMessage selectedLogMessage = ((selectedMessageField != null) ? ((LogMessage)selectedMessageField.GetValue(window)) : null);
		Text.Font = GameFont.Tiny;
		Rect rect = new Rect(inRect.x, inRect.y + buttonRowMarginTop, inRect.width, inRect.height - buttonRowMarginTop);
		WidgetRow widgetRow = new WidgetRow(rect.x, rect.y);
		WidgetRow widgetRow2 = new WidgetRow(rect.width, rect.y, UIDirection.LeftThenUp);
		GUI.DrawTexture(new Rect(inRect.x, inRect.y, inRect.width, 1f), (Texture)lineTexture);
		for (int i = 0; i < widgets.Count; i++)
		{
			LogWindowWidget logWindowWidget = widgets[i];
			try
			{
				WidgetRow widgetRow3 = ((logWindowWidget.Alignment == WidgetAlignMode.Left) ? widgetRow : widgetRow2);
				logWindowWidget.Drawer(window, inRect, selectedLogMessage, widgetRow3);
			}
			catch (Exception ex)
			{
				HugsLibController.Logger.Error("Exception while drawing log window widget: " + ex);
				widgets.RemoveAt(i);
				break;
			}
		}
		Text.Font = GameFont.Small;
	}

	private static void CopyMessage(LogMessage logMessage)
	{
		HugsLibUtility.CopyToClipboard(logMessage.text + "\n" + logMessage.StackTrace);
	}

	private static void AddOwnWidgets()
	{
		AddLogWindowWidget(delegate(Window window, Rect area, LogMessage message, WidgetRow row)
		{
			Color color = GUI.color;
			GUI.color = shareButtonColor;
			if (row.ButtonText("HugsLib_logs_shareBtn".Translate()))
			{
				HugsLibController.Instance.LogUploader.ShowPublishPrompt();
			}
			GUI.color = color;
		});
		AddLogWindowWidget(delegate(Window window, Rect area, LogMessage message, WidgetRow row)
		{
			if (row.ButtonText("HugsLib_logs_filesBtn".Translate()))
			{
				Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
				{
					new FloatMenuOption("HugsLib_logs_openLogFile".Translate(), delegate
					{
						ShellOpenLog.Execute();
					}),
					new FloatMenuOption("HugsLib_logs_openSaveDir".Translate(), delegate
					{
						ShellOpenDirectory.Execute(GenFilePaths.SaveDataFolderPath);
					}),
					new FloatMenuOption("HugsLib_logs_openModsDir".Translate(), delegate
					{
						ShellOpenDirectory.Execute("Mods");
					})
				}));
			}
		});
		AddLogWindowWidget(delegate(Window window, Rect area, LogMessage message, WidgetRow row)
		{
			if (message != null && row.ButtonText("HugsLib_logs_copy".Translate()))
			{
				CopyMessage(message);
			}
		}, WidgetAlignMode.Right);
	}
}
