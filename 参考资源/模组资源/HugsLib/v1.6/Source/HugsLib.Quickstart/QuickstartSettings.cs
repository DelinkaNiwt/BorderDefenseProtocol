using System;
using System.Xml.Serialization;
using HugsLib.Settings;

namespace HugsLib.Quickstart;

/// <summary>
/// Wraps settings related to the Quickstart system for storage in a SettingHandle.
/// </summary>
[Serializable]
public class QuickstartSettings : SettingHandleConvertible
{
	public enum QuickstartMode
	{
		Disabled,
		LoadMap,
		GenerateMap
	}

	[XmlElement]
	public QuickstartMode OperationMode = QuickstartMode.Disabled;

	[XmlElement]
	public string SaveFileToLoad;

	[XmlElement]
	public string ScenarioToGen;

	[XmlElement]
	public int MapSizeToGen = 250;

	[XmlElement]
	public bool StopOnErrors = true;

	[XmlElement]
	public bool StopOnWarnings;

	[XmlElement]
	public bool BypassSafetyDialog;

	public override void FromString(string settingValue)
	{
		SettingHandleConvertibleUtility.DeserializeValuesFromString(settingValue, this);
	}

	public override string ToString()
	{
		return SettingHandleConvertibleUtility.SerializeValuesToString(this);
	}
}
