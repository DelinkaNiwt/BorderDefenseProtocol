using System;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class MessageTool
{
	internal static void Show(string info, MessageTypeDef mt = null)
	{
		Messages.Message(info, mt ?? MessageTypeDefOf.SilentInput, historical: false);
	}

	internal static void DebugException(Exception e)
	{
		if (Prefs.DevMode)
		{
			Show(e.Message + "\n" + e.StackTrace);
		}
	}

	internal static void DebugPrint(string info)
	{
		if (Prefs.DevMode)
		{
			Show(info);
		}
	}

	internal static void ShowDialog(string s, bool doRestart = false)
	{
		if (doRestart)
		{
			WindowTool.Open(Dialog_MessageBox.CreateConfirmation(s, delegate
			{
				GameDataSaveLoader.SaveGame("autosave_last");
				GenCommandLine.Restart();
			}));
		}
		else
		{
			WindowTool.Open(Dialog_MessageBox.CreateConfirmation(s, null));
		}
	}

	internal static void ShowActionDialog(string s, Action confirmedAction, string title = null, WindowLayer layer = WindowLayer.Dialog)
	{
		WindowTool.Open(Dialog_MessageBox.CreateConfirmation(s, confirmedAction, destructive: false, title, layer));
	}

	internal static void ShowCustomDialog(string s, string title, Action onAbort, Action onConfirm, Action onNext)
	{
		if (CEditor.DontAsk)
		{
			if (onNext != null)
			{
				onNext();
			}
			else
			{
				onConfirm();
			}
		}
		else
		{
			WindowTool.Open(new CustomDialog(s, title, onAbort, onConfirm, onNext));
		}
	}
}
