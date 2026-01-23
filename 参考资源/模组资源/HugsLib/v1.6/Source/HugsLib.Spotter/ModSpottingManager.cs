using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.News;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Spotter;

/// <summary>
/// Keeps track of mod packageIds that ever were loaded together with HugsLib
/// by the player and the first/last time they were seen.
/// </summary>
public class ModSpottingManager : PersistentDataManager, IModSpotterDevActions
{
	/// <summary>
	/// Used by <see cref="T:HugsLib.Spotter.ModSpottingManager" /> to track mod packageIds loaded from the XML file. 
	/// </summary>
	private class TrackingEntry
	{
		private const string PackageIdAttributeName = "packageId";

		public string PackageId { get; }

		public bool FirstTimeSeen { get; set; }

		public static TrackingEntry FromXMLElement(XElement node)
		{
			string text = node.Attribute("packageId")?.Value;
			if (text.NullOrEmpty())
			{
				throw new FormatException("packageId not defined");
			}
			return new TrackingEntry(text);
		}

		public TrackingEntry(string packageId)
		{
			PackageId = packageId;
		}

		public XElement Serialize()
		{
			return new XElement("mod", new XAttribute("packageId", PackageId));
		}

		public override string ToString()
		{
			return "[TrackingEntry PackageId:" + PackageId + " " + string.Format("{0}:{1}]", "FirstTimeSeen", FirstTimeSeen);
		}
	}

	private readonly Dictionary<string, TrackingEntry> entries = new Dictionary<string, TrackingEntry>(StringComparer.OrdinalIgnoreCase);

	private readonly IModLogger logger = HugsLibController.Logger;

	private bool erroredOnLoad;

	private Task inspectTask;

	protected override string FileName => "SpottedMods.xml";

	protected override bool SuppressLoadSaveExceptions => false;

	internal ModSpottingManager()
	{
	}

	internal ModSpottingManager(string overrideFilePath, IModLogger logger)
	{
		this.logger = (base.DataManagerLogger = logger);
		base.OverrideFilePath = overrideFilePath;
	}

	/// <summary>
	/// Sets the "first time seen" status of a packageId until the game is restarted.
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException">Throws on null packageId</exception>
	public void SetFirstTimeSeen(string packageId, bool setFirstTimeSeen)
	{
		if (packageId == null)
		{
			throw new ArgumentNullException("packageId");
		}
		WaitForInspectionCompletion();
		bool flag = FirstTimeSeen(packageId);
		if (setFirstTimeSeen != flag)
		{
			entries[packageId] = new TrackingEntry(packageId)
			{
				FirstTimeSeen = setFirstTimeSeen
			};
		}
	}

	/// <summary>
	/// Returns true if the provided packageId was recorded for the first time during the current run.
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException">Throws on null packageId</exception>
	public bool FirstTimeSeen(string packageId)
	{
		if (packageId == null)
		{
			throw new ArgumentNullException("packageId");
		}
		WaitForInspectionCompletion();
		return entries.TryGetValue(packageId)?.FirstTimeSeen ?? false;
	}

	/// <summary>
	/// Returns true if the provided mod packageId was at any time seen running together with HugsLib.
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException">Throws on null packageId</exception>
	public bool AnytimeSeen(string packageId)
	{
		if (packageId == null)
		{
			throw new ArgumentNullException("packageId");
		}
		WaitForInspectionCompletion();
		return entries.ContainsKey(packageId);
	}

	bool IModSpotterDevActions.GetFirstTimeUserStatus(string packageId)
	{
		return FirstTimeSeen(packageId);
	}

	void IModSpotterDevActions.ToggleFirstTimeUserStatus(string packageId)
	{
		bool setFirstTimeSeen = !FirstTimeSeen(packageId);
		SetFirstTimeSeen(packageId, setFirstTimeSeen);
	}

	private void WaitForInspectionCompletion()
	{
		Task task = inspectTask;
		if (task != null && !task.Wait(TimeSpan.FromSeconds(3.0)))
		{
			throw new TaskCanceledException("Ran out of time waiting for ModSpottingManager background task completion.");
		}
	}

	internal void OnEarlyInitialize()
	{
		RunInspectPackageIdsBackgroundTask(ModsConfig.ActiveModsInLoadOrder.Select((ModMetaData m) => m.PackageIdPlayerFacing));
	}

	internal void InspectPackageIds(IEnumerable<string> packageIds)
	{
		RunInspectPackageIdsBackgroundTask(packageIds);
		WaitForInspectionCompletion();
	}

	private void RunInspectPackageIdsBackgroundTask(IEnumerable<string> packageIds)
	{
		try
		{
			string[] packageIdsArray = packageIds.ToArray();
			inspectTask = Task.Run(delegate
			{
				InspectPackageIdsSync(packageIdsArray);
			});
		}
		catch (Exception arg)
		{
			HugsLibController.Logger.Error(string.Format("Error during {0} background task: {1}", "ModSpottingManager", arg));
		}
	}

	private void InspectPackageIdsSync(string[] packageIds)
	{
		LoadEntries();
		UpdateEntriesWithPackageIds(packageIds);
		SaveEntries();
	}

	private void UpdateEntriesWithPackageIds(IEnumerable<string> packageIds)
	{
		foreach (string packageId in packageIds)
		{
			if (!entries.ContainsKey(packageId))
			{
				entries.Add(packageId, new TrackingEntry(packageId)
				{
					FirstTimeSeen = true
				});
			}
		}
	}

	protected override void LoadFromXml(XDocument xml)
	{
		entries.Clear();
		if (xml.Root == null)
		{
			throw new NullReferenceException("Missing root node");
		}
		foreach (XElement item in xml.Root.Elements())
		{
			try
			{
				TrackingEntry trackingEntry = TrackingEntry.FromXMLElement(item);
				entries[trackingEntry.PackageId] = trackingEntry;
			}
			catch (Exception arg)
			{
				throw new FormatException($"Failed to parse entry:\n{item}\nException: {arg}");
			}
		}
	}

	protected override void WriteXml(XDocument xml)
	{
		XElement xElement = new XElement("Mods");
		xml.Add(xElement);
		foreach (TrackingEntry value in entries.Values)
		{
			xElement.Add(value.Serialize());
		}
	}

	private void LoadEntries()
	{
		erroredOnLoad = false;
		try
		{
			LoadData();
		}
		catch (Exception)
		{
			erroredOnLoad = true;
		}
	}

	private void SaveEntries()
	{
		if (erroredOnLoad)
		{
			logger.Warning("Skipping ModSpottingManager saving to preserve improperly loaded file data. Fix or delete the data file and try again.");
			return;
		}
		try
		{
			SaveData();
		}
		catch (Exception)
		{
		}
	}
}
