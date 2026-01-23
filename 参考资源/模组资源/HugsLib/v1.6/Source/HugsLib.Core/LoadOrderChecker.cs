using System;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Core;

internal static class LoadOrderChecker
{
	private const string LibraryModName = "HugsLib";

	/// <summary>
	/// Ensures that the library comes after Core in the load order and displays a warning dialog otherwise.
	/// </summary>
	public static void ValidateLoadOrder()
	{
		try
		{
			bool flag = false;
			foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
			{
				if (runningMod.IsCoreMod)
				{
					flag = true;
				}
				else if (runningMod.Name == "HugsLib")
				{
					if (!flag)
					{
						ScheduleWarningDialog();
					}
					break;
				}
			}
		}
		catch (Exception e)
		{
			HugsLibController.Logger.ReportException(e);
		}
	}

	private static void ScheduleWarningDialog()
	{
		LongEventHandler.QueueLongEvent(delegate
		{
			Find.WindowStack.Add(new Dialog_Message("HugsLib_loadOrderWarning_title".Translate(), "HugsLib_loadOrderWarning_text".Translate()));
		}, null, doAsynchronously: false, null);
	}
}
