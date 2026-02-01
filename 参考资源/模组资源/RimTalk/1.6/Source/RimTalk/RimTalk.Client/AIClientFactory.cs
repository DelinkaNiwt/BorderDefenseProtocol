using System.Threading.Tasks;
using RimTalk.Client.Gemini;
using RimTalk.Client.OpenAI;
using RimTalk.Client.Player2;

namespace RimTalk.Client;

public static class AIClientFactory
{
	private static IAIClient _instance;

	private static AIProvider _currentProvider;

	public static async Task<IAIClient> GetAIClientAsync()
	{
		ApiConfig config = Settings.Get().GetActiveConfig();
		if (config == null)
		{
			return null;
		}
		if (_instance == null || _currentProvider != config.Provider)
		{
			_instance = await CreateServiceInstanceAsync(config);
			_currentProvider = config.Provider;
		}
		return _instance;
	}

	private static async Task<IAIClient> CreateServiceInstanceAsync(ApiConfig config)
	{
		string model = ((config.SelectedModel == "Custom") ? config.CustomModelName : config.SelectedModel);
		switch (config.Provider)
		{
		case AIProvider.Google:
			return new GeminiClient();
		case AIProvider.Player2:
			return await Player2Client.CreateAsync(config.ApiKey);
		case AIProvider.Local:
			return new OpenAIClient(config.BaseUrl, config.CustomModelName);
		case AIProvider.Custom:
			return new OpenAIClient(config.BaseUrl, config.CustomModelName, config.ApiKey);
		default:
		{
			if (AIProviderRegistry.Defs.TryGetValue(config.Provider, out var def))
			{
				return new OpenAIClient(def.EndpointUrl, model, config.ApiKey, def.ExtraHeaders);
			}
			return null;
		}
		}
	}

	public static void Clear()
	{
		if (_currentProvider == AIProvider.Player2)
		{
			Player2Client.StopHealthCheck();
		}
		_instance = null;
		_currentProvider = AIProvider.None;
	}
}
