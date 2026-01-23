using HugsLib.Shell;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Core;

/// <summary>
/// Handles the key presses for key bindings added by HugsLib
/// </summary>
internal static class KeyBindingHandler
{
	public static void OnGUI()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type != 4)
		{
			return;
		}
		bool flag = false;
		if (HugsLibKeyBindings.PublishLogs.JustPressed && HugsLibUtility.ControlIsHeld)
		{
			if (HugsLibUtility.AltIsHeld)
			{
				HugsLibController.Instance.LogUploader.CopyToClipboard();
			}
			else
			{
				HugsLibController.Instance.LogUploader.ShowPublishPrompt();
			}
			flag = true;
		}
		if (HugsLibKeyBindings.OpenLogFile.JustPressed)
		{
			ShellOpenLog.Execute();
			flag = true;
		}
		if (HugsLibKeyBindings.RestartRimworld.JustPressed)
		{
			GenCommandLine.Restart();
			flag = true;
		}
		if (HugsLibKeyBindings.HLOpenModSettings.JustPressed)
		{
			HugsLibUtility.OpenModSettingsDialog();
			flag = true;
		}
		if (HugsLibKeyBindings.HLOpenUpdateNews.JustPressed)
		{
			HugsLibController.Instance.UpdateFeatures.TryShowDialog(manuallyOpened: true);
			flag = true;
		}
		if (flag)
		{
			Event.current.Use();
		}
	}
}
