using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTalk.Prompt;

public class PromptPreset : IExposable
{
	public string Id = Guid.NewGuid().ToString();

	public string Name = "Default Preset";

	public string Description = "";

	public List<PromptEntry> Entries = new List<PromptEntry>();

	public bool IsActive;

	public HashSet<string> DeletedModEntryIds = new HashSet<string>();

	public PromptPreset()
	{
	}

	public PromptPreset(string name, string description = "")
	{
		Name = name;
		Description = description;
	}

	public IEnumerable<PromptEntry> GetRelativeEntries()
	{
		return Entries.Where((PromptEntry e) => e.Enabled && e.Position == PromptPosition.Relative);
	}

	public IEnumerable<PromptEntry> GetInChatEntries()
	{
		return from e in Entries
			where e.Enabled && e.Position == PromptPosition.InChat
			orderby e.InChatDepth descending
			select e;
	}

	private bool ShouldSkipModEntry(PromptEntry entry)
	{
		if (string.IsNullOrEmpty(entry.SourceModId))
		{
			return false;
		}
		if (DeletedModEntryIds.Contains(entry.Id))
		{
			return true;
		}
		if (Entries.Any((PromptEntry e) => e.Id == entry.Id))
		{
			return true;
		}
		return false;
	}

	public bool AddEntry(PromptEntry entry)
	{
		if (ShouldSkipModEntry(entry))
		{
			return false;
		}
		Entries.Add(entry);
		return true;
	}

	public bool InsertEntry(PromptEntry entry, int index)
	{
		if (ShouldSkipModEntry(entry))
		{
			return false;
		}
		if (index < 0 || index >= Entries.Count)
		{
			Entries.Add(entry);
		}
		else
		{
			Entries.Insert(index, entry);
		}
		return true;
	}

	public bool InsertEntryAfter(PromptEntry entry, string afterEntryId)
	{
		if (ShouldSkipModEntry(entry))
		{
			return false;
		}
		int index = Entries.FindIndex((PromptEntry e) => e.Id == afterEntryId);
		if (index < 0)
		{
			Entries.Add(entry);
			return false;
		}
		Entries.Insert(index + 1, entry);
		return true;
	}

	public bool InsertEntryBefore(PromptEntry entry, string beforeEntryId)
	{
		if (ShouldSkipModEntry(entry))
		{
			return false;
		}
		int index = Entries.FindIndex((PromptEntry e) => e.Id == beforeEntryId);
		if (index < 0)
		{
			Entries.Add(entry);
			return false;
		}
		Entries.Insert(index, entry);
		return true;
	}

	public string FindEntryIdByName(string entryName)
	{
		return Entries.FirstOrDefault((PromptEntry e) => e.Name == entryName)?.Id;
	}

	public bool RemoveEntry(string entryId)
	{
		PromptEntry entry = Entries.FirstOrDefault((PromptEntry e) => e.Id == entryId);
		if (entry == null)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(entry.SourceModId))
		{
			DeletedModEntryIds.Add(entry.Id);
		}
		Entries.Remove(entry);
		return true;
	}

	public void ClearBlacklist()
	{
		DeletedModEntryIds.Clear();
	}

	public PromptEntry GetEntry(string entryId)
	{
		return Entries.FirstOrDefault((PromptEntry e) => e.Id == entryId);
	}

	public void MoveEntry(string entryId, int direction)
	{
		int index = Entries.FindIndex((PromptEntry e) => e.Id == entryId);
		if (index >= 0)
		{
			int newIndex = index + direction;
			if (newIndex >= 0 && newIndex < Entries.Count)
			{
				PromptEntry entry = Entries[index];
				Entries.RemoveAt(index);
				Entries.Insert(newIndex, entry);
			}
		}
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref Id, "id", Guid.NewGuid().ToString());
		Scribe_Values.Look(ref Name, "name", "Default Preset");
		Scribe_Values.Look(ref Description, "description", "");
		Scribe_Collections.Look(ref Entries, "entries", LookMode.Deep);
		Scribe_Values.Look(ref IsActive, "isActive", defaultValue: false);
		List<string> deletedList = DeletedModEntryIds?.ToList() ?? new List<string>();
		Scribe_Collections.Look(ref deletedList, "deletedModEntryIds", LookMode.Value);
		DeletedModEntryIds = deletedList?.ToHashSet() ?? new HashSet<string>();
		if (Entries == null)
		{
			Entries = new List<PromptEntry>();
		}
		if (string.IsNullOrEmpty(Id))
		{
			Id = Guid.NewGuid().ToString();
		}
	}

	public PromptPreset Clone()
	{
		PromptPreset clone = new PromptPreset
		{
			Id = Guid.NewGuid().ToString(),
			Name = Name,
			Description = Description,
			IsActive = false,
			Entries = new List<PromptEntry>()
		};
		foreach (PromptEntry entry in Entries)
		{
			clone.Entries.Add(entry.Clone());
		}
		return clone;
	}

	public override string ToString()
	{
		return $"{Name} ({Entries.Count} entries, Active: {IsActive})";
	}
}
