using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.News;

/// <summary>
/// Stores the highest displayed update news version for all mods that provide update news via <see cref="T:HugsLib.UpdateFeatureDef" />.
/// Defs are loaded from the News folder in the root mod directory.
/// </summary>
public class UpdateFeatureManager : PersistentDataManager, IUpdateFeaturesDevActions
{
	public class IgnoredNewsIds : SettingHandleConvertible, IIgnoredNewsProviderStore
	{
		private const char SerializationSeparator = '|';

		private HashSet<string> ignoredOwnerIds = new HashSet<string>();

		public SettingHandle<IgnoredNewsIds> Handle { private get; set; }

		public override bool ShouldBeSaved => ignoredOwnerIds.Count > 0;

		public bool Contains(string ownerId)
		{
			return ignoredOwnerIds.Contains(ownerId);
		}

		public void SetIgnored(string ownerId, bool ignore)
		{
			if (ignore ? ignoredOwnerIds.Add(ownerId) : ignoredOwnerIds.Remove(ownerId))
			{
				Handle.ForceSaveChanges();
			}
		}

		public override void FromString(string settingValue)
		{
			if (!string.IsNullOrEmpty(settingValue))
			{
				ignoredOwnerIds = new HashSet<string>(settingValue.Split('|'));
			}
		}

		public override string ToString()
		{
			return ignoredOwnerIds.Join('|'.ToString());
		}
	}

	internal const string UpdateFeatureDefFolder = "News/";

	private readonly Dictionary<string, Version> highestSeenVersions = new Dictionary<string, Version>();

	protected override string FileName => "LastSeenNews.xml";

	private SettingHandle<IgnoredNewsIds> IgnoredNewsProvidersSetting { get; set; }

	private SettingHandle<bool> ShowNewsSetting { get; set; }

	public UpdateFeatureManager()
	{
		LoadData();
	}

	internal void OnEarlyInitialize()
	{
		Task<(IEnumerable<UpdateFeatureDefLoader.DefXMLNode> nodes, IEnumerable<string> loadingErrors)> loadingTask = Task.Run((Func<(IEnumerable<UpdateFeatureDefLoader.DefXMLNode>, IEnumerable<string>)>)UpdateFeatureDefLoader.LoadUpdateFeatureDefNodes);
		LongEventHandler.ExecuteWhenFinished(ResolveAndInjectNewsDefs);
		void ResolveAndInjectNewsDefs()
		{
			try
			{
				if (!loadingTask.Wait(TimeSpan.FromSeconds(3.0)))
				{
					throw new InvalidOperationException("XML loading did not resolve in time");
				}
				var (loadedNodes, errors) = loadingTask.Result;
				UpdateFeatureDefLoader.HandleDefLoadingErrors(errors);
				UpdateFeatureDefLoader.ResolveAndInjectUpdateFeatureDefs(loadedNodes);
			}
			catch (Exception ex)
			{
				HugsLibController.Logger.Error("Failed to load UpdateFeatureDefs: " + ex);
			}
		}
	}

	/// <summary>
	/// Shows the news dialog window when there are not yet displayed news items available.
	/// </summary>
	/// <param name="manuallyOpened">Pass true to disable filtering based on what has 
	/// and has not been seen and open the dialog with all available news items.</param>
	/// <returns>true, if there have been found news items that were not displayed before, and the dialog has been opened</returns>
	public bool TryShowDialog(bool manuallyOpened)
	{
		if (ShowNewsSetting.Value || manuallyOpened)
		{
			IEnumerable<UpdateFeatureDef> allDefs = DefDatabase<UpdateFeatureDef>.AllDefs;
			List<UpdateFeatureDef> list;
			bool flag;
			if (manuallyOpened)
			{
				list = allDefs.ToList();
				flag = true;
			}
			else
			{
				UpdateFeatureDef[] array = (from def in EnumerateFeatureDefsWithMoreRecentVersions(allDefs, highestSeenVersions)
					where !NewsProviderOwningModIdIsIgnored(def.OwningModId)
					select def).ToArray();
				UpdateMostRecentKnownFeatureVersions(array, highestSeenVersions);
				flag = array.Length != 0;
				IEnumerable<UpdateFeatureDef> source = FilterFeatureDefsByMatchingAudience(array, HugsLibController.Instance.ModSpotter.FirstTimeSeen, delegate(Exception e)
				{
					HugsLibController.Logger.ReportException(e);
				});
				list = source.ToList();
			}
			if (flag)
			{
				SaveData();
			}
			if (list.Count > 0)
			{
				SortFeatureDefsByModNameAndVersion(list);
				Dialog_UpdateFeatures window = (manuallyOpened ? new Dialog_UpdateFeaturesFiltered(list, IgnoredNewsProvidersSetting, this, HugsLibController.Instance.ModSpotter) : new Dialog_UpdateFeatures(list, IgnoredNewsProvidersSetting));
				Find.WindowStack.Add(window);
				return true;
			}
		}
		return false;
	}

	private static IEnumerable<UpdateFeatureDef> EnumerateFeatureDefsWithMoreRecentVersions(IEnumerable<UpdateFeatureDef> featureDefs, Dictionary<string, Version> highestSeenVersions)
	{
		foreach (UpdateFeatureDef featureDef in featureDefs)
		{
			string ownerId = featureDef.OwningModId;
			if (!ownerId.NullOrEmpty())
			{
				Version highestSeenVersion = highestSeenVersions.TryGetValue(ownerId);
				if (highestSeenVersion == null || featureDef.Version > highestSeenVersion)
				{
					yield return featureDef;
				}
			}
		}
	}

	private bool NewsProviderOwningModIdIsIgnored(string ownerId)
	{
		return IgnoredNewsProvidersSetting.Value.Contains(ownerId);
	}

	private static void UpdateMostRecentKnownFeatureVersions(IEnumerable<UpdateFeatureDef> shownNewsFeatureDefs, Dictionary<string, Version> highestSeenVersions)
	{
		foreach (UpdateFeatureDef shownNewsFeatureDef in shownNewsFeatureDefs)
		{
			string owningModId = shownNewsFeatureDef.OwningModId;
			Version version = highestSeenVersions.TryGetValue(owningModId);
			if (version == null || shownNewsFeatureDef.Version > version)
			{
				highestSeenVersions[owningModId] = shownNewsFeatureDef.Version;
			}
		}
	}

	internal static IEnumerable<UpdateFeatureDef> FilterFeatureDefsByMatchingAudience(IEnumerable<UpdateFeatureDef> featureDefs, Predicate<string> packageIdFirstTimeSeen, Action<Exception> exceptionReporter)
	{
		foreach (UpdateFeatureDef featureDef in featureDefs)
		{
			bool firstTimeSeen;
			try
			{
				string owningPackageId = featureDef.OwningPackageId;
				firstTimeSeen = packageIdFirstTimeSeen(owningPackageId);
			}
			catch (Exception obj)
			{
				exceptionReporter(obj);
				continue;
			}
			UpdateFeatureTargetAudience requiredTargetAudienceFlag = ((!firstTimeSeen) ? UpdateFeatureTargetAudience.ReturningPlayers : UpdateFeatureTargetAudience.NewPlayers);
			if ((featureDef.targetAudience & requiredTargetAudienceFlag) != 0)
			{
				yield return featureDef;
			}
		}
	}

	private static void SortFeatureDefsByModNameAndVersion(List<UpdateFeatureDef> featureDefs)
	{
		featureDefs.Sort((UpdateFeatureDef def1, UpdateFeatureDef def2) => (def1.modNameReadable != def2.modNameReadable) ? string.Compare(def1.modNameReadable, def2.modNameReadable, StringComparison.Ordinal) : def1.Version.CompareTo(def2.Version));
	}

	internal void RegisterSettings(ModSettingsPack pack)
	{
		ShowNewsSetting = pack.GetHandle("modUpdateNews", "HugsLib_setting_showNews_label".Translate(), "HugsLib_setting_showNews_desc".Translate(), defaultValue: true);
		SettingHandle<bool> handle = pack.GetHandle("showAllNews", "HugsLib_setting_allNews_label".Translate(), "HugsLib_setting_allNews_desc".Translate(), defaultValue: false);
		handle.Unsaved = true;
		handle.CustomDrawer = delegate(Rect rect)
		{
			if (Widgets.ButtonText(rect, "HugsLib_setting_allNews_button".Translate()) && !TryShowDialog(manuallyOpened: true))
			{
				Find.WindowStack.Add(new Dialog_MessageBox("HugsLib_setting_allNews_fail".Translate()));
			}
			return false;
		};
		SettingHandle<IgnoredNewsIds> ignored = pack.GetHandle<IgnoredNewsIds>("ignoredUpdateNews", "HugsLib_setting_ignoredUpdateNews_label".Translate(), null);
		IgnoredNewsProvidersSetting = ignored;
		ignored.ValueChanged += EnsureIgnoredProvidersInstance;
		EnsureIgnoredProvidersInstance(null);
		ignored.NeverVisible = true;
		ignored.Value.Handle = ignored;
		void EnsureIgnoredProvidersInstance(SettingHandle _)
		{
			if (ignored.Value == null)
			{
				ignored.Value = new IgnoredNewsIds();
				ignored.HasUnsavedChanges = false;
			}
		}
	}

	protected override void LoadFromXml(XDocument xml)
	{
		highestSeenVersions.Clear();
		if (xml.Root == null)
		{
			throw new Exception("missing root node");
		}
		foreach (XElement item in xml.Root.Elements())
		{
			highestSeenVersions.Add(item.Name.ToString(), new Version(item.Value));
		}
	}

	protected override void WriteXml(XDocument xml)
	{
		XElement xElement = new XElement("mods");
		xml.Add(xElement);
		foreach (KeyValuePair<string, Version> highestSeenVersion in highestSeenVersions)
		{
			xElement.Add(new XElement(highestSeenVersion.Key, new XText(highestSeenVersion.Value.ToString())));
		}
	}

	Version IUpdateFeaturesDevActions.GetLastSeenNewsVersion(string modIdentifier)
	{
		return highestSeenVersions.TryGetValue(modIdentifier);
	}

	IEnumerable<UpdateFeatureDef> IUpdateFeaturesDevActions.ReloadAllUpdateFeatureDefs()
	{
		UpdateFeatureDefLoader.ReloadAllUpdateFeatureDefs();
		return DefDatabase<UpdateFeatureDef>.AllDefs;
	}

	bool IUpdateFeaturesDevActions.TryShowAutomaticNewsPopupDialog()
	{
		return TryShowDialog(manuallyOpened: false);
	}

	void IUpdateFeaturesDevActions.SetLastSeenNewsVersion(string modIdentifier, Version version)
	{
		bool flag = false;
		if (version != null)
		{
			highestSeenVersions[modIdentifier] = version;
			flag = true;
		}
		else if (highestSeenVersions.ContainsKey(modIdentifier))
		{
			highestSeenVersions.Remove(modIdentifier);
			flag = true;
		}
		if (flag)
		{
			SaveData();
		}
	}
}
