using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimTalk.Data;
using RimTalk.Error;
using RimTalk.Util;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace RimTalk.Client.Gemini;

public class GeminiClient : IAIClient
{
	private readonly System.Random _random = new System.Random();

	private static string BaseUrl => AIProvider.Google.GetEndpointUrl();

	private static string CurrentApiKey => Settings.Get().GetActiveConfig()?.ApiKey;

	private static string CurrentModel => Settings.Get().GetCurrentModel();

	private static string GenerateEndpoint => BaseUrl + "/models/" + CurrentModel + ":generateContent?key=" + CurrentApiKey;

	private static string StreamEndpoint => BaseUrl + "/models/" + CurrentModel + ":streamGenerateContent?alt=sse&key=" + CurrentApiKey;

	public async Task<Payload> GetChatCompletionAsync(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<Payload> onRequestPrepared = null)
	{
		string jsonContent = BuildRequestJson(prefixMessages, messages);
		onRequestPrepared?.Invoke(new Payload(BaseUrl, CurrentModel, jsonContent, null, 0));
		string responseText = await SendRequestAsync(GenerateEndpoint, jsonContent, (DownloadHandler)new DownloadHandlerBuffer());
		GeminiResponse response = JsonUtil.DeserializeFromJson<GeminiResponse>(responseText);
		string content = response?.Candidates?[0]?.Content?.Parts?[0]?.Text;
		int tokens = (response?.UsageMetadata?.TotalTokenCount).GetValueOrDefault();
		if (response?.Candidates?[0]?.FinishReason == "MAX_TOKENS")
		{
			string msg = "Quota exceeded (MAX_TOKENS)";
			throw new QuotaExceededException(msg, new Payload(BaseUrl, CurrentModel, jsonContent, responseText, tokens, msg));
		}
		return new Payload(BaseUrl, CurrentModel, jsonContent, content, tokens);
	}

	public async Task<Payload> GetStreamingChatCompletionAsync<T>(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<T> onResponseParsed, Action<Payload> onRequestPrepared = null) where T : class
	{
		string jsonContent = BuildRequestJson(prefixMessages, messages);
		onRequestPrepared?.Invoke(new Payload(BaseUrl, CurrentModel, jsonContent, null, 0));
		JsonStreamParser<T> jsonParser = new JsonStreamParser<T>();
		GeminiStreamHandler streamHandler = new GeminiStreamHandler(delegate(string chunk)
		{
			foreach (T current in jsonParser.Parse(chunk))
			{
				onResponseParsed?.Invoke(current);
			}
		});
		await SendRequestAsync(StreamEndpoint, jsonContent, (DownloadHandler)(object)streamHandler);
		return new Payload(BaseUrl, CurrentModel, jsonContent, streamHandler.GetFullText(), streamHandler.GetTotalTokens());
	}

	private async Task<string> SendRequestAsync(string url, string jsonContent, DownloadHandler downloadHandler)
	{
		if (string.IsNullOrEmpty(CurrentApiKey))
		{
			global::RimTalk.Util.Logger.Error("API key is missing.");
			return null;
		}
		global::RimTalk.Util.Logger.Debug("API request: " + url + "\n" + jsonContent);
		UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
		try
		{
			webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
			webRequest.downloadHandler = downloadHandler;
			webRequest.SetRequestHeader("Content-Type", "application/json");
			UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();
			float inactivityTimer = 0f;
			ulong lastBytes = 0uL;
			while (!((AsyncOperation)(object)asyncOp).isDone)
			{
				if (Current.Game == null)
				{
					return null;
				}
				await Task.Delay(100);
				ulong currentBytes = webRequest.downloadedBytes;
				bool hasStartedReceiving = currentBytes != 0;
				if (currentBytes > lastBytes)
				{
					inactivityTimer = 0f;
					lastBytes = currentBytes;
				}
				else
				{
					inactivityTimer += 0.1f;
				}
				if (!hasStartedReceiving && inactivityTimer > 60f)
				{
					webRequest.Abort();
					throw new TimeoutException($"Connection timed out ({60f}s)");
				}
				if (hasStartedReceiving && inactivityTimer > 60f)
				{
					webRequest.Abort();
					throw new TimeoutException($"Read timed out ({60f}s)");
				}
			}
			string responseText = downloadHandler.text;
			GeminiStreamHandler streamHandler = default(GeminiStreamHandler);
			int num;
			if (webRequest.responseCode >= 400 || webRequest.isNetworkError || webRequest.isHttpError)
			{
				streamHandler = downloadHandler as GeminiStreamHandler;
				num = ((streamHandler != null) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				responseText = streamHandler.GetAllReceivedText();
				if (string.IsNullOrEmpty(responseText))
				{
					responseText = streamHandler.GetRawJson();
				}
			}
			if (webRequest.responseCode == 429 || webRequest.responseCode == 503)
			{
				string errorMsg = ErrorUtil.ExtractErrorMessage(responseText) ?? "Quota exceeded/Overloaded";
				throw new QuotaExceededException(errorMsg, new Payload(BaseUrl, CurrentModel, jsonContent, responseText, 0, errorMsg));
			}
			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				string errorMsg2 = ErrorUtil.ExtractErrorMessage(responseText) ?? $"Request failed: {webRequest.responseCode} - {webRequest.error}";
				global::RimTalk.Util.Logger.Error(errorMsg2);
				throw new AIRequestException(errorMsg2, new Payload(BaseUrl, CurrentModel, jsonContent, responseText, 0, errorMsg2));
			}
			if (downloadHandler is DownloadHandlerBuffer)
			{
				global::RimTalk.Util.Logger.Debug("API response: \n" + responseText);
			}
			else if (downloadHandler is GeminiStreamHandler sHandler)
			{
				global::RimTalk.Util.Logger.Debug("API response: \n" + sHandler.GetRawJson());
			}
			return responseText;
		}
		finally
		{
			((IDisposable)webRequest)?.Dispose();
		}
	}

	private string BuildRequestJson(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages)
	{
		List<(Role, string)> allMessages = (prefixMessages ?? new List<(Role, string)>()).Concat(messages ?? new List<(Role, string)>()).ToList();
		SystemInstruction systemInstruction = null;
		List<Content> contents = new List<Content>();
		if (allMessages.Count > 0 && allMessages[0].Item1 == Role.System)
		{
			string instruction = allMessages[0].Item2;
			if (CurrentModel.Contains("gemma"))
			{
				contents.Add(new Content
				{
					Role = "user",
					Parts = new List<Part>
					{
						new Part
						{
							Text = $"{_random.Next()} {instruction}"
						}
					}
				});
			}
			else
			{
				systemInstruction = new SystemInstruction
				{
					Parts = new List<Part>
					{
						new Part
						{
							Text = instruction
						}
					}
				};
			}
			allMessages.RemoveAt(0);
		}
		contents.AddRange(allMessages.Select<(Role, string), Content>(((Role role, string message) m) => new Content
		{
			Role = ((m.role == Role.User) ? "user" : "model"),
			Parts = new List<Part>
			{
				new Part
				{
					Text = m.message
				}
			}
		}));
		GenerationConfig config = new GenerationConfig();
		if (CurrentModel.Contains("flash"))
		{
			config.ThinkingConfig = new ThinkingConfig
			{
				ThinkingBudget = 0
			};
		}
		return JsonUtil.SerializeToJson(new GeminiDto
		{
			SystemInstruction = systemInstruction,
			Contents = contents,
			GenerationConfig = config
		});
	}

	public static async Task<List<string>> FetchModelsAsync(string apiKey, string url)
	{
		UnityWebRequest webRequest = UnityWebRequest.Get(url + "?key=" + apiKey);
		try
		{
			UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();
			while (!((AsyncOperation)(object)asyncOp).isDone)
			{
				await Task.Delay(100);
			}
			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				global::RimTalk.Util.Logger.Error("Failed to fetch Google models: " + webRequest.error);
				return new List<string>();
			}
			try
			{
				return (from m in JsonUtil.DeserializeFromJson<GoogleModelsResponse>(webRequest.downloadHandler.text)?.Models?.Where((GoogleModelData m) => m.SupportedGenerationMethods?.Contains("generateContent") ?? false)
					select m.Name.StartsWith("models/") ? m.Name.Substring(7) : m.Name into m
					orderby m
					select m).ToList() ?? new List<string>();
			}
			catch (Exception ex)
			{
				global::RimTalk.Util.Logger.Error("Failed to parse Google models: " + ex.Message);
				return new List<string>();
			}
		}
		finally
		{
			((IDisposable)webRequest)?.Dispose();
		}
	}
}
