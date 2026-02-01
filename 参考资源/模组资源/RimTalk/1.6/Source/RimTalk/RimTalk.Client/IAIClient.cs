using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimTalk.Data;

namespace RimTalk.Client;

public interface IAIClient
{
	Task<Payload> GetChatCompletionAsync(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<Payload> onRequestPrepared = null);

	Task<Payload> GetStreamingChatCompletionAsync<T>(List<(Role role, string message)> prefixMessages, List<(Role role, string message)> messages, Action<T> onResponseParsed, Action<Payload> onRequestPrepared = null) where T : class;
}
