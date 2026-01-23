using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Core;

/// <summary>
/// Checks the current version of the library against the About.xml -&gt; requiredLibraryVersion of all mods.
/// Shows a popup window (<see cref="T:HugsLib.Core.Dialog_LibraryUpdateRequired" />) if one of the loaded mods requires a
/// more recent version of the library. 
/// </summary>
internal class LibraryVersionChecker
{
	public struct VersionMismatchReport
	{
		public string ModName { get; }

		public Version ExpectedVersion { get; }

		public VersionMismatchReport(string modName, Version expectedVersion)
		{
			ModName = modName;
			ExpectedVersion = expectedVersion;
		}
	}

	private class EnumerateRequiredLibraryVersionsInMods : IEnumerable<(string, Version)>, IEnumerable
	{
		public IEnumerator<(string, Version)> GetEnumerator()
		{
			foreach (ModContentPack contentPack in LoadedModManager.RunningMods)
			{
				Version requiredVersion = VersionFile.TryParseVersionFile(contentPack)?.RequiredLibraryVersion;
				if (requiredVersion != null)
				{
					yield return (contentPack.Name, requiredVersion);
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private readonly Version currentLibraryVersion;

	private readonly IModLogger logger;

	internal IEnumerable<(string modName, Version requiredVersion)> RequiredLibraryVersionEnumerator { get; set; } = new EnumerateRequiredLibraryVersionsInMods();

	public LibraryVersionChecker(Version currentLibraryVersion, IModLogger logger)
	{
		this.currentLibraryVersion = currentLibraryVersion;
		this.logger = logger;
	}

	public void OnEarlyInitialize()
	{
		Task<VersionMismatchReport?> versionCheckTask = RunVersionCheckAsync();
		LongEventHandler.QueueLongEvent(delegate
		{
			ShowVersionMismatchDialogIfNeeded(versionCheckTask);
		}, null, doAsynchronously: false, null);
	}

	private void ShowVersionMismatchDialogIfNeeded(Task<VersionMismatchReport?> versionCheckTask)
	{
		VersionMismatchReport? versionMismatchReport = TryWaitForTaskResult(versionCheckTask, TimeSpan.FromSeconds(1.0));
		if (versionMismatchReport.HasValue)
		{
			VersionMismatchReport valueOrDefault = versionMismatchReport.GetValueOrDefault();
			if (true)
			{
				Find.WindowStack.Add(new Dialog_LibraryUpdateRequired(valueOrDefault.ModName, valueOrDefault.ExpectedVersion));
			}
		}
	}

	internal Task<VersionMismatchReport?> RunVersionCheckAsync()
	{
		return Task.Run(delegate
		{
			VersionMismatchReport? result = null;
			try
			{
				var (modName, version) = RequiredLibraryVersionEnumerator.OrderByDescending(((string modName, Version requiredVersion) t) => t.requiredVersion).FirstOrDefault();
				if (version != null && version > currentLibraryVersion)
				{
					result = new VersionMismatchReport(modName, version);
					return result;
				}
			}
			catch (Exception e)
			{
				logger.ReportException(e);
			}
			return result;
		});
	}

	internal VersionMismatchReport? TryWaitForTaskResult(Task<VersionMismatchReport?> task, TimeSpan waitTime)
	{
		try
		{
			bool flag = task.Wait(waitTime);
			if (flag && task.IsCompleted)
			{
				return task.Result;
			}
			if (!flag)
			{
				throw new Exception("Ran out of time waiting for LibraryVersionChecker background task completion.");
			}
		}
		catch (Exception e)
		{
			logger.ReportException(e);
		}
		return null;
	}
}
