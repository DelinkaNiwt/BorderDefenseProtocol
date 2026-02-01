using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Data;
using RimTalk.Error;
using RimTalk.Source.Data;
using RimTalk.Util;

namespace RimTalk.Service;

public static class AIService
{
	private static bool _busy;

	private static bool _firstInstruction = true;

	public static async Task ChatStreaming(TalkRequest request, Action<TalkResponse> onPlayerResponseReceived)
	{
		List<(Role role, string content)> prefixMessages = request.PromptMessages ?? new List<(Role, string)>();
		ApiLog apiLog = ApiHistory.AddRequest(request, Channel.Stream);
		ApiLog lastApiLog = apiLog;
		HandleFinalStatus(payload: await ExecuteWithRetry(apiLog, async (IAIClient client) => await client.GetStreamingChatCompletionAsync(prefixMessages, new List<(Role, string)>(), delegate(TalkResponse response)
		{
			if (Cache.GetByName(response.Name) != null)
			{
				response.TalkType = request.TalkType;
				int num = (int)(DateTime.Now - lastApiLog.Timestamp).TotalMilliseconds;
				if (lastApiLog == apiLog)
				{
					num -= lastApiLog.ElapsedMs;
				}
				ApiLog apiLog2 = ApiHistory.AddResponse(apiLog.Id, response.Text, response.Name, response.InteractionRaw, null, num);
				response.Id = apiLog2.Id;
				lastApiLog = apiLog2;
				onPlayerResponseReceived?.Invoke(response);
			}
		}, delegate(Payload prep)
		{
			ApiHistory.UpdatePayload(apiLog.Id, prep);
		})), apiLog: apiLog);
		_firstInstruction = false;
	}

	public static async Task<T> Query<T>(TalkRequest request) where T : class, IJsonData
	{
		List<(Role role, string message)> messages = new List<(Role, string)> { (Role.User, request.Prompt) };
		List<(Role role, string message)> prefixMessages = new List<(Role, string)> { (Role.System, request.Context) };
		ApiLog apiLog = ApiHistory.AddRequest(request, Channel.Query);
		Payload payload = await ExecuteWithRetry(apiLog, async (IAIClient client) => await client.GetChatCompletionAsync(prefixMessages, messages, delegate(Payload prep)
		{
			ApiHistory.UpdatePayload(apiLog.Id, prep);
		}));
		if (string.IsNullOrEmpty(payload.Response) || !string.IsNullOrEmpty(payload.ErrorMessage))
		{
			ApiHistory.UpdatePayload(apiLog.Id, payload);
			return null;
		}
		try
		{
			T data = JsonUtil.DeserializeFromJson<T>(payload.Response);
			ApiHistory.AddResponse(apiLog.Id, data.GetText(), null, null, payload);
			return data;
		}
		catch (Exception)
		{
			ReportError(apiLog, payload, "Json Deserialization Failed");
			return null;
		}
	}

	private static async Task<Payload> ExecuteWithRetry(ApiLog apiLog, Func<IAIClient, Task<Payload>> action)
	{
		_busy = true;
		try
		{
			Exception capturedEx = null;
			Payload payload = await AIErrorHandler.HandleWithRetry(async delegate
			{
				IAIClient client = await AIClientFactory.GetAIClientAsync();
				return await action(client);
			}, delegate(Exception ex)
			{
				capturedEx = ex;
				apiLog.Response = ex.Message;
				apiLog.IsError = true;
			});
			if (payload == null)
			{
				payload = ((capturedEx is AIRequestException { Payload: not null } rex) ? rex.Payload : new Payload("Unknown", "Unknown", "", null, 0, capturedEx?.Message ?? "Unknown Error"));
			}
			else
			{
				Stats.IncrementCalls();
				Stats.IncrementTokens(payload.TokenCount);
			}
			return payload;
		}
		finally
		{
			_busy = false;
		}
	}

	private static void HandleFinalStatus(ApiLog apiLog, Payload payload)
	{
		if (string.IsNullOrEmpty(apiLog.Response) && !apiLog.IsError && string.IsNullOrEmpty(payload.ErrorMessage))
		{
			ReportError(apiLog, payload, "Json Deserialization Failed");
		}
		else
		{
			ApiHistory.UpdatePayload(apiLog.Id, payload);
		}
	}

	private static void ReportError(ApiLog apiLog, Payload payload, string errorMsg)
	{
		apiLog.Response = errorMsg + "\n\nRaw Response:\n" + payload.Response;
		apiLog.IsError = true;
		payload.ErrorMessage = errorMsg;
		ApiHistory.UpdatePayload(apiLog.Id, payload);
	}

	public static bool IsFirstInstruction()
	{
		return _firstInstruction;
	}

	public static bool IsBusy()
	{
		return _busy;
	}

	public static void Clear()
	{
		_busy = false;
		_firstInstruction = true;
	}
}
