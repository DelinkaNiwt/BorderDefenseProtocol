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

namespace RimTalk.Client.OpenAI;

public class OpenAIClient : IAIClient
{
	private const string DefaultPath = "/v1/chat/completions";

	private readonly string _endpointUrl;

	public OpenAIClient(string baseUrl, string model, string apiKey = null, Dictionary<string, string> extraHeaders = null)
	{
		_003Cmodel_003EP = model;
		_003CapiKey_003EP = apiKey;
		_003CextraHeaders_003EP = extraHeaders;
		_endpointUrl = FormatEndpointUrl(baseUrl);
		base._002Ector();
	}

	private static string FormatEndpointUrl(string baseUrl)
	{
		if (string.IsNullOrEmpty(baseUrl))
		{
			return string.Empty;
		}
		string trimmed = baseUrl.Trim().TrimEnd('/');
		Uri uri = new Uri(trimmed);
		return (uri.AbsolutePath == "/" || string.IsNullOrEmpty(uri.AbsolutePath.Trim('/'))) ? (trimmed + "/v1/chat/completions") : trimmed;
	}

	public async Task<Payload> GetChatCompletionAsync(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<Payload> onRequestPrepared = null)
	{
		string jsonContent = BuildRequestJson(prefixMessages, messages, stream: false);
		onRequestPrepared?.Invoke(new Payload(_endpointUrl, _003Cmodel_003EP, jsonContent, null, 0));
		OpenAIResponse response = JsonUtil.DeserializeFromJson<OpenAIResponse>(await SendRequestAsync(jsonContent, (DownloadHandler)new DownloadHandlerBuffer()));
		return new Payload(response: response?.Choices?[0]?.Message?.Content, tokenCount: (response?.Usage?.TotalTokens).GetValueOrDefault(), url: _endpointUrl, model: _003Cmodel_003EP, request: jsonContent);
	}

	public async Task<Payload> GetStreamingChatCompletionAsync<T>(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<T> onResponseParsed, Action<Payload> onRequestPrepared = null) where T : class
	{
		string jsonContent = BuildRequestJson(prefixMessages, messages, stream: true);
		onRequestPrepared?.Invoke(new Payload(_endpointUrl, _003Cmodel_003EP, jsonContent, null, 0));
		JsonStreamParser<T> jsonParser = new JsonStreamParser<T>();
		OpenAIStreamHandler streamHandler = new OpenAIStreamHandler(delegate(string chunk)
		{
			foreach (T current in jsonParser.Parse(chunk))
			{
				onResponseParsed?.Invoke(current);
			}
		});
		await SendRequestAsync(jsonContent, (DownloadHandler)(object)streamHandler);
		return new Payload(_endpointUrl, _003Cmodel_003EP, jsonContent, streamHandler.GetFullText(), streamHandler.GetTotalTokens());
	}

	private string BuildRequestJson(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, bool stream)
	{
		List<Message> allMessages = new List<Message>();
		if (prefixMessages != null)
		{
			allMessages.AddRange(prefixMessages.Select(((Role role, string message) m) => new Message
			{
				Role = RoleToString(m.role),
				Content = m.message
			}));
		}
		allMessages.AddRange(messages.Select(((Role role, string message) m) => new Message
		{
			Role = RoleToString(m.role),
			Content = m.message
		}));
		OpenAIRequest request = new OpenAIRequest
		{
			Model = _003Cmodel_003EP,
			Messages = allMessages,
			Stream = stream,
			StreamOptions = (stream ? new StreamOptions
			{
				IncludeUsage = true
			} : null)
		};
		return JsonUtil.SerializeToJson(request);
	}

	private static string RoleToString(Role role)
	{
		if (1 == 0)
		{
		}
		string result = role switch
		{
			Role.System => "system", 
			Role.User => "user", 
			Role.AI => "assistant", 
			_ => "user", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task<string> SendRequestAsync(string jsonContent, DownloadHandler downloadHandler)
	{
		if (string.IsNullOrEmpty(_endpointUrl))
		{
			global::RimTalk.Util.Logger.Error("Endpoint URL is missing.");
			return null;
		}
		global::RimTalk.Util.Logger.Debug("API request: " + _endpointUrl + "\n" + jsonContent);
		UnityWebRequest webRequest = new UnityWebRequest(_endpointUrl, "POST");
		try
		{
			webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
			webRequest.downloadHandler = downloadHandler;
			webRequest.SetRequestHeader("Content-Type", "application/json");
			if (!string.IsNullOrEmpty(_003CapiKey_003EP))
			{
				webRequest.SetRequestHeader("Authorization", "Bearer " + _003CapiKey_003EP);
			}
			if (_003CextraHeaders_003EP != null)
			{
				foreach (KeyValuePair<string, string> header in _003CextraHeaders_003EP)
				{
					webRequest.SetRequestHeader(header.Key, header.Value);
				}
			}
			UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();
			bool isLocal = _endpointUrl.Contains("localhost") || _endpointUrl.Contains("127.0.0.1") || _endpointUrl.Contains("192.168.") || _endpointUrl.Contains("10.");
			float inactivityTimer = 0f;
			ulong lastBytes = 0uL;
			float connectTimeout = (isLocal ? 300f : 60f);
			float readTimeout = 60f;
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
				if (!hasStartedReceiving && inactivityTimer > connectTimeout)
				{
					webRequest.Abort();
					throw new TimeoutException($"Connection timed out (Waited {connectTimeout}s for first token)");
				}
				if (hasStartedReceiving && inactivityTimer > readTimeout)
				{
					webRequest.Abort();
					throw new TimeoutException($"Read timed out (Stalled for {readTimeout}s during generation)");
				}
			}
			string responseText = downloadHandler.text;
			OpenAIStreamHandler sHandler = default(OpenAIStreamHandler);
			int num;
			if (webRequest.responseCode >= 400 || webRequest.isNetworkError || webRequest.isHttpError)
			{
				sHandler = downloadHandler as OpenAIStreamHandler;
				num = ((sHandler != null) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				responseText = sHandler.GetAllReceivedText();
				if (string.IsNullOrEmpty(responseText))
				{
					responseText = sHandler.GetRawJson();
				}
			}
			if (webRequest.responseCode == 429)
			{
				string errorMsg = ErrorUtil.ExtractErrorMessage(responseText) ?? "Quota exceeded";
				throw new QuotaExceededException(errorMsg, new Payload(_endpointUrl, _003Cmodel_003EP, jsonContent, responseText, 0, errorMsg));
			}
			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				string errorMsg2 = ErrorUtil.ExtractErrorMessage(responseText) ?? webRequest.error;
				global::RimTalk.Util.Logger.Error($"Request failed: {webRequest.responseCode} - {errorMsg2}");
				throw new AIRequestException(errorMsg2, new Payload(_endpointUrl, _003Cmodel_003EP, jsonContent, responseText, 0, errorMsg2));
			}
			if (downloadHandler is DownloadHandlerBuffer)
			{
				global::RimTalk.Util.Logger.Debug("API response: \n" + responseText);
			}
			else if (downloadHandler is OpenAIStreamHandler sh)
			{
				global::RimTalk.Util.Logger.Debug("API response: \n" + sh.GetRawJson());
			}
			return responseText;
		}
		finally
		{
			((IDisposable)webRequest)?.Dispose();
		}
	}

	public static async Task<List<string>> FetchModelsAsync(string apiKey, string url)
	{
		UnityWebRequest webRequest = UnityWebRequest.Get(url);
		try
		{
			webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
			UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();
			while (!((AsyncOperation)(object)asyncOp).isDone)
			{
				await Task.Delay(100);
			}
			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				global::RimTalk.Util.Logger.Error("Failed to fetch models: " + webRequest.error);
				return new List<string>();
			}
			return JsonUtil.DeserializeFromJson<OpenAIModelsResponse>(webRequest.downloadHandler.text)?.Data?.Select((Model m) => m.Id).ToList() ?? new List<string>();
		}
		finally
		{
			((IDisposable)webRequest)?.Dispose();
		}
	}
}
