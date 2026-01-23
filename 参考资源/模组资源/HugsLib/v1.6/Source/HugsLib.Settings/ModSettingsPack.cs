using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Settings;

/// <summary>
/// A group of settings values added by a mod. Each mod has their own ModSettingsPack.
/// Loaded values are stored until they are "claimed" by their mod by requesting a handle for a setting with the same name.
/// </summary>
public class ModSettingsPack
{
	private readonly Dictionary<string, string> loadedValues = new Dictionary<string, string>();

	private readonly List<SettingHandle> handles = new List<SettingHandle>();

	/// <summary>
	/// Identifier of the mod that owns this pack
	/// </summary>
	public string ModId { get; private set; }

	/// <summary>
	/// The name of the owning mod that will display is the Mod Settings dialog
	/// </summary>
	public string EntryName { get; set; }

	/// <summary>
	/// Additional context menu options for this entry in the mod settings dialog.
	/// Will be shown when the hovering menu button for this entry is clicked.
	/// </summary>
	public IEnumerable<ContextMenuEntry> ContextMenuEntries { get; set; }

	/// <summary>
	/// Returns true if any handles retrieved from this pack have had their values changed.
	/// Resets to false after the changes are saved.
	/// </summary>
	public bool HasUnsavedChanges
	{
		get
		{
			for (int i = 0; i < handles.Count; i++)
			{
				if (handles[i].HasUnsavedChanges)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Enumerates the handles that have been registered with this pack up to this point.
	/// </summary>
	public IEnumerable<SettingHandle> Handles => handles;

	internal ModSettingsManager ParentManager { get; set; }

	internal bool CanBeReset => handles.Any((SettingHandle h) => h.CanBeReset);

	internal ModSettingsPack(string modId)
	{
		ModId = modId;
	}

	/// <summary>
	/// Retrieves an existing SettingHandle from the pack, or creates a new one.
	/// Loaded settings will only display in the Mod Settings dialog after they have been claimed using this method.
	/// </summary>
	/// <typeparam name="T">The type of setting value you are creating.</typeparam>
	/// <param name="settingName">Unique identifier for the setting. Must be unique for this specific pack only.</param>
	/// <param name="title">A display name for the setting that will show up next to it in the Mod Settings dialog. Recommended to keep this short.</param>
	/// <param name="description">A description for the setting that will appear in a tooltip when the player hovers over the setting in the Mod Settings dialog.</param>
	/// <param name="defaultValue">The value the setting will assume when newly created and when the player resets the setting to its default.</param>
	/// <param name="validator">An optional delegate that will be called when a new value is about to be assigned to the handle. Receives a string argument and must return a bool to indicate if the passed value is valid for the setting.</param>
	/// <param name="enumPrefix">Used only for Enum settings. Enum values are displayed in a readable format by the following method: Translate(prefix+EnumValueName)</param>
	public SettingHandle<T> GetHandle<T>(string settingName, string title, string description, T defaultValue = default(T), SettingHandle.ValueIsValid validator = null, string enumPrefix = null)
	{
		if (!PersistentDataManager.IsValidElementName(settingName))
		{
			throw new Exception("Invalid name for mod setting: " + settingName);
		}
		SettingHandle<T> settingHandle = null;
		for (int i = 0; i < handles.Count; i++)
		{
			if (!(handles[i].Name != settingName) && handles[i] is SettingHandle<T>)
			{
				settingHandle = (SettingHandle<T>)handles[i];
				break;
			}
		}
		if (settingHandle == null)
		{
			settingHandle = new SettingHandle<T>(settingName)
			{
				Value = defaultValue
			};
			settingHandle.ParentPack = this;
			handles.Add(settingHandle);
		}
		settingHandle.DefaultValue = defaultValue;
		settingHandle.Title = title;
		settingHandle.Description = description;
		settingHandle.Validator = validator;
		settingHandle.EnumStringPrefix = enumPrefix;
		if (typeof(T).IsEnum && (enumPrefix == null || !(enumPrefix + Enum.GetName(typeof(T), defaultValue)).CanTranslate()))
		{
			HugsLibController.Logger.Warning("Missing enum setting labels for enum " + typeof(T));
		}
		if (loadedValues.ContainsKey(settingName))
		{
			string text = loadedValues[settingName];
			loadedValues.Remove(settingName);
			settingHandle.StringValue = text;
			if (settingHandle.Validator != null && !settingHandle.Validator(text))
			{
				settingHandle.ResetToDefault();
			}
		}
		settingHandle.HasUnsavedChanges = false;
		return settingHandle;
	}

	/// <summary>
	/// Returns a handle that was already created.
	/// Will return null if the handle does not exist yet.
	/// </summary>
	/// <exception cref="T:System.InvalidCastException">Throws an exception if the referenced handle does not match the provided type</exception>
	/// <param name="settingName">The name of the handle to retrieve</param>
	public SettingHandle<T> GetHandle<T>(string settingName)
	{
		for (int i = 0; i < handles.Count; i++)
		{
			SettingHandle settingHandle = handles[i];
			if (settingHandle.Name == settingName)
			{
				if (!(settingHandle is SettingHandle<T>))
				{
					throw new InvalidCastException($"Handle {settingName} does not match the specified type of {typeof(SettingHandle<T>)}");
				}
				return (SettingHandle<T>)settingHandle;
			}
		}
		return null;
	}

	/// <summary>
	/// Attempts to retrieve a setting value by name.
	/// If a handle for that value has already been created, returns that handle's StringValue.
	/// Otherwise will return the unclaimed value that was loaded from the XML file.
	/// Will return null if the value does not exist.
	/// </summary>
	/// <param name="settingName">The name of the setting the value of which should be retrieved</param>
	public string PeekValue(string settingName)
	{
		SettingHandle settingHandle = handles.Find((SettingHandle h) => h.Name == settingName);
		if (settingHandle != null)
		{
			return settingHandle.StringValue;
		}
		if (loadedValues.TryGetValue(settingName, out var value))
		{
			return value;
		}
		return null;
	}

	/// <summary>
	/// Returns true, if there is a setting value that can be retrieved with PeekValue.
	/// This includes already created handles and unclaimed values.
	/// </summary>
	/// <param name="settingName">The name of the setting to check</param>
	public bool ValueExists(string settingName)
	{
		return handles.Find((SettingHandle h) => h.Name == settingName) != null || loadedValues.ContainsKey(settingName);
	}

	/// <summary>
	/// Deletes a setting loaded from the xml file before it is claimed using GetHandle.
	/// Useful for cleaning up settings that are no longer in use.
	/// </summary>
	/// <param name="name">The identifier of the setting (handle identifier)</param>
	public bool TryRemoveUnclaimedValue(string name)
	{
		return loadedValues.Remove(name);
	}

	/// <summary>
	/// Prompts the <see cref="T:HugsLib.Settings.ModSettingsManager" /> to save changes if any or the registered 
	/// <see cref="T:HugsLib.Settings.ModSettingsPack" />s have handles with unsaved changes
	/// </summary>
	public void SaveChanges()
	{
		ParentManager.SaveChanges();
	}

	internal void LoadFromXml(XElement xml)
	{
		loadedValues.Clear();
		foreach (XElement item in xml.Elements())
		{
			loadedValues.Add(item.Name.ToString(), item.Value);
		}
	}

	internal void WriteXml(XElement xml)
	{
		if (loadedValues.Count == 0 && handles.Count((SettingHandle h) => !h.Unsaved && !h.HasDefaultValue()) == 0)
		{
			return;
		}
		XElement xElement = new XElement(ModId);
		xml.Add(xElement);
		foreach (KeyValuePair<string, string> loadedValue in loadedValues)
		{
			xElement.Add(new XElement(loadedValue.Key, new XText(loadedValue.Value)));
		}
		foreach (SettingHandle handle in handles)
		{
			handle.HasUnsavedChanges = false;
			if (handle.ShouldBeSaved)
			{
				xElement.Add(new XElement(handle.Name, new XText(handle.StringValue)));
			}
		}
	}

	public override string ToString()
	{
		return "[ModSettingsPack ModId:" + ModId + " Handles:" + Handles.Select((SettingHandle h) => h.Name).Join(",") + "]";
	}
}
