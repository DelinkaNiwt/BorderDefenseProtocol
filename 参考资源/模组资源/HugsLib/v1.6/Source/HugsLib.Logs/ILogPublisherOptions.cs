namespace HugsLib.Logs;

public interface ILogPublisherOptions
{
	bool UseCustomOptions { get; set; }

	bool UseUrlShortener { get; set; }

	bool IncludePlatformInfo { get; set; }

	bool AllowUnlimitedLogSize { get; set; }

	string AuthToken { get; set; }
}
