using System;
using System.Collections.Generic;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Utils;

namespace HugsLib.Settings;

/// <summary>
/// A central place for mods to store persistent settings. Individual settings are grouped by mod using ModSettingsPack
/// </summary>
public class ModSettingsManager : PersistentDataManager
{
	private readonly List<ModSettingsPack> packs = new List<ModSettingsPack>();

	protected override string FileName => "ModSettings.xml";

	/// <summary>
	/// Enumerates the <see cref="T:HugsLib.Settings.ModSettingsPack" />s that have been registered up to this point.
	/// </summary>
	public IEnumerable<ModSettingsPack> ModSettingsPacks => packs;

	/// <summary>
	/// Returns true when there are handles with values that have changed since the last time settings were saved.
	/// </summary>
	public bool HasUnsavedChanges
	{
		get
		{
			for (int i = 0; i < packs.Count; i++)
			{
				if (packs[i].HasUnsavedChanges)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Fires when <see cref="M:HugsLib.Settings.ModSettingsManager.SaveChanges" /> is called and changes are about to be saved.
	/// Use <see cref="P:HugsLib.Settings.ModSettingsManager.ModSettingsPacks" /> and <see cref="P:HugsLib.Settings.ModSettingsPack.HasUnsavedChanges" /> to identify changed packs,
	/// and <see cref="P:HugsLib.Settings.ModSettingsPack.Handles" /> with <see cref="P:HugsLib.Settings.SettingHandle.HasUnsavedChanges" /> to identify changed handles.
	/// </summary>
	public event Action BeforeModSettingsSaved;

	/// <summary>
	/// Fires when <see cref="M:HugsLib.Settings.ModSettingsManager.SaveChanges" /> is called and the settings file has just been written to disk.
	/// </summary>
	public event Action AfterModSettingsSaved;

	internal ModSettingsManager()
	{
		LoadData();
	}

	internal ModSettingsManager(string overrideFilePath, IModLogger logger)
	{
		base.OverrideFilePath = overrideFilePath;
		base.DataManagerLogger = logger;
		LoadData();
	}

	/// <summary>
	/// Retrieves the <see cref="T:HugsLib.Settings.ModSettingsPack" /> for a given mod identifier.
	/// </summary>
	/// <param name="modId">The unique identifier of the mod that owns the pack</param>
	/// <param name="displayModName">If not null, assigns the <see cref="P:HugsLib.Settings.ModSettingsPack.EntryName" /> property of the pack.
	/// This will be displayed in the Mod Settings dialog as a header.</param>
	public ModSettingsPack GetModSettings(string modId, string displayModName = null)
	{
		if (!PersistentDataManager.IsValidElementName(modId))
		{
			throw new Exception("Invalid name for mod settings group: " + modId);
		}
		ModSettingsPack modSettingsPack = null;
		for (int i = 0; i < packs.Count; i++)
		{
			if (packs[i].ModId == modId)
			{
				modSettingsPack = packs[i];
				break;
			}
		}
		if (modSettingsPack == null)
		{
			modSettingsPack = InstantiatePack(modId);
		}
		if (displayModName != null)
		{
			modSettingsPack.EntryName = displayModName;
		}
		return modSettingsPack;
	}

	/// <summary>
	/// Saves all settings to disk and notifies all ModBase mods by calling SettingsChanged() 
	/// </summary>
	public void SaveChanges()
	{
		if (!HasUnsavedChanges)
		{
			return;
		}
		try
		{
			this.BeforeModSettingsSaved?.Invoke();
		}
		catch (Exception e)
		{
			HugsLibController.Logger.ReportException(e);
		}
		SaveData();
		try
		{
			this.AfterModSettingsSaved?.Invoke();
		}
		catch (Exception e2)
		{
			HugsLibController.Logger.ReportException(e2);
		}
	}

	public bool HasSettingsForMod(string modId)
	{
		return packs.Find((ModSettingsPack p) => p.ModId == modId) != null;
	}

	/// <summary>
	/// Removes a settings pack for a mod if it exists. Use SaveChanges to apply the change afterward.
	/// </summary>
	/// <param name="modId">The identifier of the mod owning the pack</param>
	public bool TryRemoveModSettings(string modId)
	{
		ModSettingsPack modSettingsPack = packs.Find((ModSettingsPack p) => p.ModId == modId);
		if (modSettingsPack == null)
		{
			return false;
		}
		if (packs.Remove(GetModSettings(modId)))
		{
			return true;
		}
		return false;
	}

	protected override void LoadFromXml(XDocument xml)
	{
		packs.Clear();
		if (xml.Root == null)
		{
			throw new NullReferenceException("Missing root node");
		}
		foreach (XElement item in xml.Root.Elements())
		{
			ModSettingsPack modSettingsPack = InstantiatePack(item.Name.ToString());
			modSettingsPack.LoadFromXml(item);
		}
	}

	protected override void WriteXml(XDocument xml)
	{
		XElement xElement = new XElement("settings");
		xml.Add(xElement);
		foreach (ModSettingsPack pack in packs)
		{
			pack.WriteXml(xElement);
		}
	}

	private ModSettingsPack InstantiatePack(string modId)
	{
		ModSettingsPack modSettingsPack = new ModSettingsPack(modId)
		{
			ParentManager = this
		};
		packs.Add(modSettingsPack);
		return modSettingsPack;
	}
}
