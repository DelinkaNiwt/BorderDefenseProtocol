using System;
using System.Xml.Serialization;
using HugsLib.Settings;

namespace HugsLib.Logs;

[Serializable]
public class LogPublisherOptions : SettingHandleConvertible, IEquatable<LogPublisherOptions>, ILogPublisherOptions
{
	[XmlElement]
	public bool UseCustomOptions { get; set; }

	[XmlElement]
	public bool UseUrlShortener { get; set; }

	[XmlElement]
	public bool IncludePlatformInfo { get; set; }

	[XmlElement]
	public bool AllowUnlimitedLogSize { get; set; }

	[XmlElement]
	public string AuthToken { get; set; }

	public override bool ShouldBeSaved => !object.Equals(this, new LogPublisherOptions());

	public override void FromString(string settingValue)
	{
		SettingHandleConvertibleUtility.DeserializeValuesFromString(settingValue, this);
	}

	public override string ToString()
	{
		return SettingHandleConvertibleUtility.SerializeValuesToString(this);
	}

	public bool Equals(LogPublisherOptions other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		return UseCustomOptions == other.UseCustomOptions && UseUrlShortener == other.UseUrlShortener && IncludePlatformInfo == other.IncludePlatformInfo && AllowUnlimitedLogSize == other.AllowUnlimitedLogSize && AuthToken == other.AuthToken;
	}
}
