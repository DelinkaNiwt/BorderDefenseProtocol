using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class WindowTool
{
	internal static string toggleText;

	internal static int MaxH => (UI.screenHeight < 768) ? UI.screenHeight : ((UI.screenHeight < 1200) ? 768 : 800);

	internal static int MaxHS => 700;

	internal static int ToolW => 320;

	internal static Vector2 DefaultToolWindow => new Vector2(ToolW, MaxH);

	internal static void SimpleCloseButton(Window w)
	{
		SZWidgets.ButtonText(RAcceptButton(w), "Close".Translate(), delegate
		{
			w.Close();
		});
	}

	internal static void SimpleAcceptButton(Window w, Action action)
	{
		SZWidgets.ButtonText(RAcceptButton(w), "OK".Translate(), action);
	}

	internal static void SimpleSaveButton(Window w, Action action)
	{
		SZWidgets.ButtonText(RAcceptButton(w), "Save".Translate(), action);
	}

	internal static void SimpleAcceptAndExtend(Window w, Action aOk, Action aReset, Action aResetAll, Action aSave, int widthExtended, string customLabel)
	{
		w.windowRect.width = (CEditor.IsExtendedUI ? ((float)widthExtended) : DefaultToolWindow.x);
		toggleText = (CEditor.IsExtendedUI ? "<<" : ">>");
		int num = X_Accept(w) - 30;
		int num2 = Y_Accept(w);
		int num3 = 30;
		Rect rect = new Rect(num, num2, 100f, num3);
		SZWidgets.ButtonText(rect, customLabel, aOk);
		num += 100;
		SZWidgets.ButtonText(num, num2, num3, num3, toggleText, delegate
		{
			CEditor.IsExtendedUI = !CEditor.IsExtendedUI;
		});
		num += num3;
		if (CEditor.IsExtendedUI)
		{
			int num4 = widthExtended - (int)DefaultToolWindow.x;
			int num5 = num4 / 3;
			SZWidgets.ButtonText(num, num2, num5, num3, Label.RESET, aReset);
			num += num5;
			SZWidgets.ButtonText(num, num2, num5, num3, Label.RESETALL, aResetAll);
			num += num5;
			SZWidgets.ButtonText(num, num2, num5, num3, Label.SAVE, aSave);
		}
	}

	internal static void SimpleCustomButton(Window w, int xPos, Action action, string label, string tooltip, int width = 100)
	{
		int num = Y_Accept(w);
		int num2 = 30;
		Rect rect = new Rect(xPos, num, width, num2);
		SZWidgets.ButtonText(rect, label, action, tooltip);
	}

	internal static Rect RAcceptButton(Window w)
	{
		return new Rect(X_Accept(w), Y_Accept(w), 100f, 30f);
	}

	internal static int X_Accept(Window w)
	{
		return (int)w.InitialSize.x - 136;
	}

	internal static int Y_Accept(Window w)
	{
		return (int)w.InitialSize.y - 66;
	}

	internal static void Open(Window w)
	{
		w.layer = CEditor.Layer;
		Find.WindowStack.Add(w);
	}

	internal static void TopLayerForWindowOf<T>(bool force) where T : Window
	{
		Window w = Find.WindowStack.WindowOfType<T>();
		BringToFront(w, force);
	}

	internal static void TopLayerForWindowOfType(string type, bool force)
	{
		Window windowOfType = GetWindowOfType(type);
		BringToFront(windowOfType, force);
	}

	internal static void BringToFront(Window w, bool force)
	{
		if (w != null)
		{
			if (w.layer != CEditor.Layer)
			{
				w.layer = CEditor.Layer;
				GUI.BringWindowToFront(w.ID);
			}
			else if (force)
			{
				GUI.BringWindowToFront(w.ID);
			}
		}
	}

	internal static void BringToFrontMulti(List<Window> l)
	{
		if (l.NullOrEmpty())
		{
			return;
		}
		l.Reverse();
		foreach (Window item in l)
		{
			BringToFront(item, force: false);
		}
	}

	internal static int GetIndex(Window w)
	{
		return Find.WindowStack.Windows.IndexOf(w);
	}

	internal static Window GetWindowOf<T>() where T : Window
	{
		return Find.WindowStack.WindowOfType<T>();
	}

	internal static bool IsOpen<T>() where T : Window
	{
		return Find.WindowStack.IsOpen(typeof(T));
	}

	internal static void TryRemove<T>() where T : Window
	{
		Find.WindowStack.TryRemove(typeof(T));
	}

	internal static Window GetWindowOfType(string type)
	{
		foreach (Window window in Find.WindowStack.Windows)
		{
			if (window.GetType().ToString() == type)
			{
				return window;
			}
		}
		return null;
	}

	internal static void ShowOpenedWindows()
	{
		foreach (Window window in Find.WindowStack.Windows)
		{
			MessageTool.Show(window.GetType().ToString());
		}
	}

	internal static List<Window> GetWindowOfStartsWithType(string type)
	{
		List<Window> list = new List<Window>();
		foreach (Window window in Find.WindowStack.Windows)
		{
			if (window.GetType().ToString().StartsWith(type))
			{
				list.Add(window);
			}
		}
		return list;
	}

	internal static List<Window> GetWindowOfEndsWithType(string type)
	{
		List<Window> list = new List<Window>();
		foreach (Window window in Find.WindowStack.Windows)
		{
			if (window.GetType().ToString().EndsWith(type))
			{
				list.Add(window);
			}
		}
		return list;
	}

	internal static void Close(List<Window> l)
	{
		if (l.NullOrEmpty())
		{
			return;
		}
		foreach (Window item in l)
		{
			item.Close();
		}
	}
}
