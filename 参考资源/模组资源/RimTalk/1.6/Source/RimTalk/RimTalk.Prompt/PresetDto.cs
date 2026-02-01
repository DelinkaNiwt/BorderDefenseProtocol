using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Prompt;

[DataContract]
public class PresetDto
{
	[DataMember(Name = "version")]
	public int Version { get; set; } = 1;

	[DataMember(Name = "name")]
	public string Name { get; set; }

	[DataMember(Name = "description")]
	public string Description { get; set; }

	[DataMember(Name = "entries")]
	public List<EntryDto> Entries { get; set; } = new List<EntryDto>();

	public static PresetDto FromPreset(PromptPreset preset)
	{
		if (preset == null)
		{
			return null;
		}
		PresetDto dto = new PresetDto
		{
			Version = 1,
			Name = preset.Name,
			Description = preset.Description,
			Entries = new List<EntryDto>()
		};
		foreach (PromptEntry entry in preset.Entries)
		{
			dto.Entries.Add(EntryDto.FromEntry(entry));
		}
		return dto;
	}

	public PromptPreset ToPreset()
	{
		PromptPreset preset = new PromptPreset
		{
			Id = Guid.NewGuid().ToString(),
			Name = (Name ?? "Imported Preset"),
			Description = (Description ?? ""),
			IsActive = false,
			Entries = new List<PromptEntry>()
		};
		if (Entries != null)
		{
			foreach (EntryDto entryDto in Entries)
			{
				PromptEntry entry = entryDto.ToEntry();
				if (entry != null)
				{
					if (!entry.IsMainChatHistory && entry.Content?.Trim() == "{{chat.history}}")
					{
						entry.IsMainChatHistory = true;
					}
					preset.Entries.Add(entry);
				}
			}
		}
		return preset;
	}
}
