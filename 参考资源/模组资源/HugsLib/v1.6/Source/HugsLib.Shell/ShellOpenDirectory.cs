using System;
using System.IO;
using HugsLib.Utils;
using UnityEngine;

namespace HugsLib.Shell;

/// <summary>
/// A command to open a directory in the systems default file explorer.
/// Since Unity's OpenUrl() is broken on OS X, we can use a shell to do it correctly.
/// </summary>
public static class ShellOpenDirectory
{
	public static bool Execute(string directory)
	{
		string text = ParsePath(directory);
		if (string.IsNullOrEmpty(text))
		{
			HugsLibController.Logger.Warning("Attempted to open a directory but none was set.");
			return false;
		}
		if (PlatformUtility.GetCurrentPlatform() == PlatformType.MacOSX)
		{
			return Shell.StartProcess(new Shell.ShellCommand
			{
				FileName = "open",
				Args = directory
			});
		}
		Application.OpenURL(text);
		return false;
	}

	private static string ParsePath(string path)
	{
		if (path.StartsWith("~\\") || path.StartsWith("~/"))
		{
			path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path.Remove(0, 2));
		}
		if (!path.StartsWith("\"") && !path.EndsWith("\""))
		{
			return path.SurroundWithDoubleQuotes();
		}
		return path;
	}
}
