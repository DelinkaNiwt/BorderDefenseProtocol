using System.IO;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Shell;

/// <summary>
/// A Command to open the log file in the systems default text editor.
/// </summary>
public static class ShellOpenLog
{
	public static bool Execute()
	{
		string text = HugsLibUtility.TryGetLogFilePath();
		if (text.NullOrEmpty() || !File.Exists(text))
		{
			HugsLibController.Logger.ReportException(new FileNotFoundException("Log file path is unknown or log file does not exist. Path:" + text));
			return false;
		}
		switch (PlatformUtility.GetCurrentPlatform())
		{
		case PlatformType.Linux:
			return Shell.StartProcess(new Shell.ShellCommand
			{
				FileName = text
			});
		case PlatformType.MacOSX:
			return Shell.StartProcess(new Shell.ShellCommand
			{
				FileName = "open",
				Args = text
			});
		case PlatformType.Windows:
			return Shell.StartProcess(new Shell.ShellCommand
			{
				FileName = text
			});
		default:
			HugsLibController.Logger.ReportException(new Shell.UnsupportedPlatformException("ShellOpenLog"));
			return false;
		}
	}
}
