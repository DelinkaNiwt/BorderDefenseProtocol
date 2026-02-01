using System;
using System.Runtime.Serialization;

namespace RimTalk.Prompt;

[DataContract]
public class EntryDto
{
	[DataMember(Name = "name")]
	public string Name { get; set; }

	[DataMember(Name = "content")]
	public string Content { get; set; }

	[DataMember(Name = "role")]
	public string Role { get; set; }

	[DataMember(Name = "position")]
	public string Position { get; set; }

	[DataMember(Name = "inChatDepth")]
	public int InChatDepth { get; set; }

	[DataMember(Name = "enabled")]
	public bool Enabled { get; set; } = true;

	[DataMember(Name = "isMainChatHistory")]
	public bool IsMainChatHistory { get; set; }

	public static EntryDto FromEntry(PromptEntry entry)
	{
		if (entry == null)
		{
			return null;
		}
		return new EntryDto
		{
			Name = entry.Name,
			Content = entry.Content,
			Role = entry.Role.ToString(),
			Position = entry.Position.ToString(),
			InChatDepth = entry.InChatDepth,
			Enabled = entry.Enabled,
			IsMainChatHistory = entry.IsMainChatHistory
		};
	}

	public PromptEntry ToEntry()
	{
		PromptEntry entry = new PromptEntry
		{
			Id = Guid.NewGuid().ToString(),
			Name = (Name ?? "New Prompt"),
			Content = (Content ?? ""),
			InChatDepth = InChatDepth,
			Enabled = Enabled,
			IsMainChatHistory = IsMainChatHistory,
			SourceModId = null
		};
		if (!string.IsNullOrEmpty(Role) && Enum.TryParse<PromptRole>(Role, ignoreCase: true, out var role))
		{
			entry.Role = role;
		}
		if (!string.IsNullOrEmpty(Position) && Enum.TryParse<PromptPosition>(Position, ignoreCase: true, out var pos))
		{
			entry.Position = pos;
		}
		return entry;
	}
}
