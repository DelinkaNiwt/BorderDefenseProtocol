using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimTalk.Data;
using RimTalk.Error;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace RimTalk.Client.Player2;

public class Player2Client : IAIClient
{
	private const string GameClientId = "019a8368-b00b-72bc-b367-2825079dc6fb";

	private const string LocalUrl = "http://localhost:4315";

	private readonly string _apiKey;

	private readonly bool _isLocalConnection;

	private static DateTime _lastHealthCheck = DateTime.MinValue;

	private static bool _healthCheckActive;

	private static string RemoteUrl => AIProvider.Player2.GetEndpointUrl();

	private string CurrentApiUrl => _isLocalConnection ? "http://localhost:4315" : RemoteUrl;

	private Player2Client(string apiKey, bool isLocal)
	{
		_apiKey = apiKey;
		_isLocalConnection = isLocal;
		if (!_healthCheckActive && !string.IsNullOrEmpty(apiKey) && !isLocal)
		{
			_healthCheckActive = true;
			StartHealthCheckLoop();
		}
	}

	public static async Task<Player2Client> CreateAsync(string fallbackApiKey = null)
	{
		try
		{
			string localKey = await TryGetLocalPlayer2Key();
			if (!string.IsNullOrEmpty(localKey))
			{
				global::RimTalk.Util.Logger.Debug("Player2 local app detected.");
				ShowNotification("RimTalk.Player2.LocalDetected", MessageTypeDefOf.PositiveEvent);
				return new Player2Client(localKey, isLocal: true);
			}
			if (!string.IsNullOrEmpty(fallbackApiKey))
			{
				global::RimTalk.Util.Logger.Debug("Using manual Player2 API key.");
				return new Player2Client(fallbackApiKey, isLocal: false);
			}
			ShowNotification("RimTalk.Player2.LocalNotFound", MessageTypeDefOf.CautionInput);
			throw new Exception("Player2 not available: no local app and no API key.");
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			global::RimTalk.Util.Logger.Error("Failed to create Player2 client: " + ex2.Message);
			throw;
		}
	}

	public async Task<Payload> GetChatCompletionAsync(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<Payload> onRequestPrepared = null)
	{
		await EnsureHealthCheck();
		string jsonContent = BuildRequestJson(prefixMessages, messages, stream: false);
		onRequestPrepared?.Invoke(new Payload(CurrentApiUrl, null, jsonContent, null, 0));
		Player2Response response = JsonUtil.DeserializeFromJson<Player2Response>(await SendRequestAsync(CurrentApiUrl + "/v1/chat/completions", jsonContent, (DownloadHandler)new DownloadHandlerBuffer()));
		return new Payload(response: response?.Choices?[0]?.Message?.Content, tokenCount: (response?.Usage?.TotalTokens).GetValueOrDefault(), url: CurrentApiUrl, model: null, request: jsonContent);
	}

	public async Task<Payload> GetStreamingChatCompletionAsync<T>(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<T> onResponseParsed, Action<Payload> onRequestPrepared = null) where T : class
	{
		await EnsureHealthCheck();
		string jsonContent = BuildRequestJson(prefixMessages, messages, stream: true);
		onRequestPrepared?.Invoke(new Payload(CurrentApiUrl, null, jsonContent, null, 0));
		JsonStreamParser<T> jsonParser = new JsonStreamParser<T>();
		Player2StreamHandler streamHandler = new Player2StreamHandler(delegate(string chunk)
		{
			foreach (T current in jsonParser.Parse(chunk))
			{
				onResponseParsed?.Invoke(current);
			}
		});
		await SendRequestAsync(CurrentApiUrl + "/v1/chat/completions", jsonContent, (DownloadHandler)(object)streamHandler);
		return new Payload(CurrentApiUrl, null, jsonContent, streamHandler.GetFullText(), streamHandler.GetTotalTokens());
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
		return JsonUtil.SerializeToJson(new Player2Request
		{
			Messages = allMessages,
			Stream = stream
		});
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

	private async Task<string> SendRequestAsync(string url, string jsonContent, DownloadHandler downloadHandler)
	{
		global::RimTalk.Util.Logger.Debug("Player2 Request (" + (_isLocalConnection ? "local" : "remote") + "): " + url + "\n" + jsonContent);
		UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
		try
		{
			webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
			webRequest.downloadHandler = downloadHandler;
			webRequest.SetRequestHeader("Content-Type", "application/json");
			webRequest.SetRequestHeader("Authorization", "Bearer " + _apiKey);
			webRequest.SetRequestHeader("X-Game-Client-Id", "019a8368-b00b-72bc-b367-2825079dc6fb");
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
			if (downloadHandler is Player2StreamHandler sHandler)
			{
				sHandler.Flush();
				if (!string.IsNullOrEmpty(sHandler.DetectedError))
				{
					string errorMsg = sHandler.DetectedError;
					string allText = sHandler.GetAllReceivedText();
					if (errorMsg.Contains("ResourceExhausted") || errorMsg.Contains("Insufficient"))
					{
						throw new QuotaExceededException("Player2 quota exceeded", new Payload(url, null, jsonContent, allText, 0, errorMsg));
					}
					throw new AIRequestException(errorMsg, new Payload(url, null, jsonContent, allText, 0, errorMsg));
				}
			}
			string responseText = downloadHandler.text;
			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				string errorMsg2 = ErrorUtil.ExtractErrorMessage(responseText) ?? webRequest.error;
				global::RimTalk.Util.Logger.Error($"Player2 failed: {webRequest.responseCode} - {errorMsg2}");
				throw new AIRequestException(errorMsg2, new Payload(url, null, jsonContent, responseText, 0, errorMsg2));
			}
			if (downloadHandler is DownloadHandlerBuffer)
			{
				global::RimTalk.Util.Logger.Debug("Player2 Response: \n" + responseText);
			}
			else if (downloadHandler is Player2StreamHandler sh)
			{
				global::RimTalk.Util.Logger.Debug($"Player2 Streaming complete. Tokens: {sh.GetTotalTokens()}");
			}
			return responseText;
		}
		finally
		{
			((IDisposable)webRequest)?.Dispose();
		}
	}

	private static async Task<string> TryGetLocalPlayer2Key()
	{
		try
		{
			global::RimTalk.Util.Logger.Debug("Checking for local Player2 app...");
			UnityWebRequest healthRequest = UnityWebRequest.Get("http://localhost:4315/v1/health");
			try
			{
				healthRequest.timeout = 2;
				await SendWebRequestAsync(healthRequest);
				if (healthRequest.isNetworkError || healthRequest.isHttpError)
				{
					global::RimTalk.Util.Logger.Debug("Player2 local app health check failed: " + healthRequest.error);
					return null;
				}
				global::RimTalk.Util.Logger.Debug("Player2 local app health check passed");
			}
			finally
			{
				((IDisposable)healthRequest)?.Dispose();
			}
			UnityWebRequest loginRequest = new UnityWebRequest("http://localhost:4315/v1/login/web/019a8368-b00b-72bc-b367-2825079dc6fb", "POST");
			try
			{
				loginRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
				loginRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
				loginRequest.SetRequestHeader("Content-Type", "application/json");
				loginRequest.timeout = 3;
				await SendWebRequestAsync(loginRequest);
				if (loginRequest.isNetworkError || loginRequest.isHttpError)
				{
					global::RimTalk.Util.Logger.Debug($"Player2 local login failed: {loginRequest.responseCode} - {loginRequest.error}");
					return null;
				}
				LocalPlayer2Response response = JsonUtil.DeserializeFromJson<LocalPlayer2Response>(loginRequest.downloadHandler.text);
				if (!string.IsNullOrEmpty(response?.P2Key))
				{
					global::RimTalk.Util.Logger.Message("[Player2] ✓ Local app authenticated successfully");
					return response.P2Key;
				}
				global::RimTalk.Util.Logger.Warning("Player2 local app responded but no API key in response");
				return null;
			}
			finally
			{
				((IDisposable)loginRequest)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			global::RimTalk.Util.Logger.Debug("Local Player2 detection failed: " + ex2.Message);
			return null;
		}
	}

	private static Task SendWebRequestAsync(UnityWebRequest request)
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		((AsyncOperation)(object)request.SendWebRequest()).completed += delegate
		{
			tcs.SetResult(result: true);
		};
		return tcs.Task;
	}

	private static void ShowNotification(string messageKey, MessageTypeDef type)
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			try
			{
				bool flag = messageKey == "RimTalk.Player2.LocalDetected";
				string text = (flag ? "RimTalk: Player2 desktop app detected! Using automatic authentication (no API key needed)." : "RimTalk: Player2 desktop app not found. Please start app or add API key manually.");
				Messages.Message(text, type);
				global::RimTalk.Util.Logger.Message(flag ? "RimTalk: ✓ Successfully connected to local Player2 app" : "RimTalk: Player2 local app not available, manual API key required");
			}
			catch
			{
			}
		});
	}

	private async void StartHealthCheckLoop()
	{
		while (_healthCheckActive && Current.Game != null)
		{
			await Task.Delay(60000);
			if (_healthCheckActive)
			{
				await EnsureHealthCheck(force: true);
			}
		}
	}

	private async Task EnsureHealthCheck(bool force = false)
	{
		if (_isLocalConnection || string.IsNullOrEmpty(_apiKey) || (!force && (DateTime.Now - _lastHealthCheck).TotalSeconds < 60.0))
		{
			return;
		}
		try
		{
			UnityWebRequest webRequest = new UnityWebRequest(RemoteUrl + "/v1/health", "GET");
			try
			{
				webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
				webRequest.SetRequestHeader("Authorization", "Bearer " + _apiKey);
				webRequest.SetRequestHeader("X-Game-Client-Id", "019a8368-b00b-72bc-b367-2825079dc6fb");
				UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();
				while (!((AsyncOperation)(object)asyncOp).isDone)
				{
					if (Current.Game == null)
					{
						return;
					}
					await Task.Delay(100);
				}
				_lastHealthCheck = DateTime.Now;
				if (webRequest.responseCode == 200)
				{
					global::RimTalk.Util.Logger.Debug("Player2 health check successful");
				}
				else
				{
					global::RimTalk.Util.Logger.Warning($"Player2 health check failed: {webRequest.responseCode} - {webRequest.error}");
				}
			}
			finally
			{
				((IDisposable)webRequest)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			global::RimTalk.Util.Logger.Warning("Player2 health check exception: " + ex.Message);
		}
	}

	public static void StopHealthCheck()
	{
		_healthCheckActive = false;
	}

	public static void CheckPlayer2StatusAndNotify()
	{
		Task.Run(async delegate
		{
			bool isAvailable = await IsPlayer2LocalAppAvailableAsync();
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				if (isAvailable)
				{
					Messages.Message("RimTalk: Player2 desktop app detected!", MessageTypeDefOf.PositiveEvent);
				}
				else
				{
					Messages.Message("RimTalk: Player2 desktop app not detected.", MessageTypeDefOf.CautionInput);
				}
			});
		});
	}

	private static async Task<bool> IsPlayer2LocalAppAvailableAsync()
	{
		try
		{
			UnityWebRequest webRequest = UnityWebRequest.Get("http://localhost:4315/v1/health");
			try
			{
				webRequest.timeout = 2;
				await SendWebRequestAsync(webRequest);
				return webRequest.responseCode == 200;
			}
			finally
			{
				((IDisposable)webRequest)?.Dispose();
			}
		}
		catch
		{
			return false;
		}
	}
}
