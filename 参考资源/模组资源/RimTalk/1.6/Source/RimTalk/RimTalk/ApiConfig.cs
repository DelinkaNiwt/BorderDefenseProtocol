using Verse;

namespace RimTalk;

public class ApiConfig : IExposable
{
	public bool IsEnabled = true;

	public AIProvider Provider = AIProvider.Google;

	public string ApiKey = "";

	public string SelectedModel = "(choose model)";

	public string CustomModelName = "";

	public string BaseUrl = "";

	public void ExposeData()
	{
		Scribe_Values.Look(ref IsEnabled, "isEnabled", defaultValue: true);
		Scribe_Values.Look(ref Provider, "provider", AIProvider.Google);
		Scribe_Values.Look(ref ApiKey, "apiKey", "");
		Scribe_Values.Look(ref SelectedModel, "selectedModel", "gemma-3-27b-it");
		Scribe_Values.Look(ref CustomModelName, "customModelName", "");
		Scribe_Values.Look(ref BaseUrl, "baseUrl", "");
	}

	public bool IsValid()
	{
		if (!IsEnabled)
		{
			return false;
		}
		if (Settings.Get().UseCloudProviders)
		{
			if (Provider == AIProvider.Player2)
			{
				return SelectedModel != "(choose model)";
			}
			return !string.IsNullOrWhiteSpace(ApiKey) && SelectedModel != "(choose model)";
		}
		return !string.IsNullOrWhiteSpace(BaseUrl);
	}
}
