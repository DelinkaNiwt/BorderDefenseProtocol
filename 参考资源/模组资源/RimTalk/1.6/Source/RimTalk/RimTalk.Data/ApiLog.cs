using System;
using System.Text;
using RimTalk.Client;
using RimTalk.Source.Data;

namespace RimTalk.Data;

public class ApiLog
{
	public enum State
	{
		None,
		Pending,
		Ignored,
		Spoken,
		Failed
	}

	public string InteractionType;

	public bool IsFirstDialogue;

	public int ElapsedMs;

	public Guid Id { get; } = Guid.NewGuid();

	public int ConversationId { get; set; }

	public TalkRequest TalkRequest { get; set; } = talkRequest ?? new TalkRequest(null, null);

	public string Name { get; set; }

	public string Response { get; set; }

	public Payload Payload { get; set; }

	public DateTime Timestamp { get; }

	public int SpokenTick { get; set; }

	public bool IsError { get; set; }

	public Channel Channel { get; set; }

	public ApiLog(string name, TalkRequest talkRequest, string response, Payload payload, DateTime timestamp, Channel channel)
	{
		Name = name;
		Response = response;
		Payload = payload;
		Timestamp = timestamp;
		SpokenTick = 0;
		Channel = channel;
		base._002Ector();
	}

	public State GetState()
	{
		if (IsError)
		{
			return State.Failed;
		}
		if (SpokenTick == -1 || Channel == Channel.Query)
		{
			return State.Ignored;
		}
		if (Response == null || SpokenTick == 0)
		{
			return State.Pending;
		}
		if (Response != null && SpokenTick > 0)
		{
			return State.Spoken;
		}
		return State.None;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}");
		sb.AppendLine("Pawn: " + (Name ?? "-"));
		sb.AppendLine("InteractionType: " + (InteractionType ?? "-"));
		sb.AppendLine($"ElapsedMs: {ElapsedMs}");
		sb.AppendLine($"TokenCount: {Payload?.TokenCount}");
		sb.AppendLine($"SpokenTick: {SpokenTick}");
		sb.AppendLine();
		sb.AppendLine("=== Prompt ===");
		sb.AppendLine(TalkRequest.Prompt ?? string.Empty);
		sb.AppendLine();
		sb.AppendLine("=== Response ===");
		sb.AppendLine(Response ?? string.Empty);
		sb.AppendLine();
		sb.AppendLine("=== Contexts ===");
		sb.AppendLine(TalkRequest.Context);
		return sb.ToString().TrimEnd();
	}
}
