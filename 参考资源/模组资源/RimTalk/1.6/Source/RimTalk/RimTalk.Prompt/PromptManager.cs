using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Util;
using Verse;

namespace RimTalk.Prompt;

public class PromptManager : IExposable
{
	private static PromptManager _instance;

	public List<PromptPreset> Presets = new List<PromptPreset>();

	public VariableStore VariableStore = new VariableStore();

	private PromptPreset _simplePresetCache;

	private string _simplePresetCacheKey = "";

	public static PromptManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new PromptManager();
			}
			return _instance;
		}
	}

	public static PromptContext LastContext { get; private set; }

	public PromptPreset GetActivePreset()
	{
		if (Presets.Count == 0)
		{
			EnsureInitialized();
		}
		PromptPreset active = Presets.FirstOrDefault((PromptPreset p) => p.IsActive);
		if (active == null && Presets.Count > 0)
		{
			Presets[0].IsActive = true;
			return Presets[0];
		}
		return active;
	}

	public void SetActivePreset(string presetId)
	{
		foreach (PromptPreset preset in Presets)
		{
			preset.IsActive = preset.Id == presetId;
		}
	}

	public void AddPreset(PromptPreset preset)
	{
		Presets.Add(preset);
	}

	public bool RemovePreset(string presetId)
	{
		PromptPreset preset = Presets.FirstOrDefault((PromptPreset p) => p.Id == presetId);
		if (preset != null)
		{
			Presets.Remove(preset);
			if (preset.IsActive && Presets.Count > 0)
			{
				Presets[0].IsActive = true;
			}
			return true;
		}
		return false;
	}

	public PromptPreset DuplicatePreset(string presetId)
	{
		PromptPreset source = Presets.FirstOrDefault((PromptPreset p) => p.Id == presetId);
		if (source == null)
		{
			return null;
		}
		PromptPreset clone = source.Clone();
		string baseName = source.Name;
		Match match = Regex.Match(baseName, "^(.*?)\\s*\\((\\d+)\\)$");
		if (match.Success)
		{
			baseName = match.Groups[1].Value.Trim();
		}
		clone.Name = GetUniqueName(baseName);
		Presets.Add(clone);
		return clone;
	}

	public PromptPreset CreateNewPreset(string baseName)
	{
		PromptPreset preset = CreateDefaultPreset();
		preset.IsActive = false;
		preset.Name = GetUniqueName(baseName);
		Presets.Add(preset);
		return preset;
	}

	public string GetUniqueName(string baseName, string excludeId = null)
	{
		if (!Presets.Any((PromptPreset p) => p.Name == baseName && p.Id != excludeId))
		{
			return baseName;
		}
		int i = 1;
		string newName;
		do
		{
			newName = $"{baseName} ({i++})";
		}
		while (Presets.Any((PromptPreset p) => p.Name == newName && p.Id != excludeId));
		return newName;
	}

	private static List<(PromptRole role, string content)> MergeConsecutiveRoles(List<(PromptRole role, string content)> messages)
	{
		if (messages == null || messages.Count <= 1)
		{
			return messages;
		}
		List<(PromptRole, string)> merged = new List<(PromptRole, string)>();
		foreach (var (role, content) in messages)
		{
			if (merged.Count > 0)
			{
				if (merged[merged.Count - 1].Item1 == role)
				{
					(PromptRole, string) last = merged[merged.Count - 1];
					merged[merged.Count - 1] = (role, last.Item2 + "\n\n" + content);
					continue;
				}
			}
			merged.Add((role, content));
		}
		return merged;
	}

	public static Role ConvertToRole(PromptRole promptRole)
	{
		return (Role)promptRole;
	}

	public void InitializeDefaults()
	{
		if (Presets.Count == 0)
		{
			PromptPreset defaultPreset = CreateDefaultPreset();
			Presets.Add(defaultPreset);
		}
	}

	public void EnsureInitialized()
	{
		if (Presets.Count == 0 && LanguageDatabase.activeLanguage != null)
		{
			InitializeDefaults();
		}
	}

	private PromptPreset CreateDefaultPreset()
	{
		RimTalkSettings settings = Settings.Get();
		string baseInstruction = (string.IsNullOrWhiteSpace(settings.CustomInstruction) ? Constant.DefaultInstruction : settings.CustomInstruction);
		return new PromptPreset
		{
			Name = "RimTalk Default",
			Description = "RimTalk default prompt preset",
			IsActive = true,
			Entries = new List<PromptEntry>
			{
				new PromptEntry
				{
					Name = "Base Instruction",
					Role = PromptRole.System,
					Position = PromptPosition.Relative,
					Content = baseInstruction
				},
				new PromptEntry
				{
					Name = "JSON Format",
					Role = PromptRole.System,
					Position = PromptPosition.Relative,
					Content = "{{json.format}}"
				},
				new PromptEntry
				{
					Name = "Pawn Profiles",
					Role = PromptRole.System,
					Position = PromptPosition.Relative,
					Content = "{{context}}"
				},
				new PromptEntry
				{
					Name = "Chat History",
					Role = PromptRole.User,
					Position = PromptPosition.Relative,
					IsMainChatHistory = true,
					Content = "{{chat.history}}"
				},
				new PromptEntry
				{
					Name = "Dialogue Prompt",
					Role = PromptRole.User,
					Position = PromptPosition.Relative,
					Content = "{{prompt}}"
				}
			}
		};
	}

	public void MigrateLegacyInstruction(string legacyInstruction)
	{
		if (!string.IsNullOrWhiteSpace(legacyInstruction))
		{
			PromptPreset preset = GetActivePreset();
			if (preset != null && !preset.Entries.Any((PromptEntry e) => e.Name == "Legacy Custom Instruction"))
			{
				preset.Entries.Insert(1, new PromptEntry
				{
					Name = "Legacy Custom Instruction",
					Role = PromptRole.System,
					Position = PromptPosition.Relative,
					Content = legacyInstruction
				});
				Logger.Debug("Migrated legacy custom instruction to new prompt system");
			}
		}
	}

	public void ResetToDefaults()
	{
		Presets.Clear();
		VariableStore.Clear();
		InitializeDefaults();
		foreach (PromptPreset preset in Presets)
		{
			preset.ClearBlacklist();
		}
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref Presets, "presets", LookMode.Deep);
		Scribe_Deep.Look(ref VariableStore, "variableStore");
		if (Presets == null)
		{
			Presets = new List<PromptPreset>();
		}
		if (VariableStore == null)
		{
			VariableStore = new VariableStore();
		}
		if (Scribe.mode != LoadSaveMode.PostLoadInit && Scribe.mode != LoadSaveMode.LoadingVars)
		{
			return;
		}
		foreach (PromptPreset preset in Presets)
		{
			if (!preset.Entries.Any((PromptEntry e) => e.IsMainChatHistory))
			{
				PromptEntry legacyEntry = preset.Entries.FirstOrDefault((PromptEntry e) => e.Content.Trim() == "{{chat.history}}");
				if (legacyEntry != null)
				{
					legacyEntry.IsMainChatHistory = true;
				}
			}
		}
	}

	public static void SetInstance(PromptManager manager)
	{
		_instance = manager;
	}

	public List<(Role role, string content)> BuildMessages(TalkRequest talkRequest, List<Pawn> pawns, string status)
	{
		RimTalkSettings settings = Settings.Get();
		string dialogueType = PromptContextProvider.GetDialogueTypeString(talkRequest, pawns);
		talkRequest.Context = PromptService.BuildContext(pawns);
		PromptService.DecoratePrompt(talkRequest, pawns, status);
		PromptContext context = PromptContext.FromTalkRequest(talkRequest, pawns);
		context.DialogueType = dialogueType;
		context.DialogueStatus = status;
		context.DialoguePrompt = talkRequest.Prompt;
		LastContext = context;
		PromptPreset preset;
		if (settings.UseAdvancedPromptMode)
		{
			preset = GetActivePreset();
		}
		else
		{
			string cacheKey = settings.CustomInstruction ?? "";
			if (_simplePresetCache == null || _simplePresetCacheKey != cacheKey)
			{
				_simplePresetCache = CreateDefaultPreset();
				_simplePresetCacheKey = cacheKey;
			}
			preset = _simplePresetCache;
		}
		if (preset == null)
		{
			preset = CreateDefaultPreset();
		}
		List<PromptMessageSegment> segments = new List<PromptMessageSegment>();
		List<(PromptRole, string)> messages = BuildMessagesFromPreset(preset, context, segments);
		talkRequest.PromptMessageSegments = ((segments.Count > 0) ? segments : null);
		return messages.Select<(PromptRole, string), (Role, string)>(((PromptRole role, string content) m) => ((Role)m.role, content: m.content)).ToList();
	}

	private List<(PromptRole role, string content)> BuildMessagesFromPreset(PromptPreset preset, PromptContext context, List<PromptMessageSegment> segments)
	{
		List<(PromptRole, string)> result = new List<(PromptRole, string)>();
		int lastHistoryIndex = 0;
		var systemParts = (from e in preset.Entries
			where e.Enabled && e.Role == PromptRole.System && e.Position == PromptPosition.Relative
			select new
			{
				e = e,
				content = ScribanParser.Render(e.Content, context)
			} into x
			where !string.IsNullOrWhiteSpace(x.content)
			select x).ToList();
		foreach (var item in systemParts)
		{
			segments?.Add(new PromptMessageSegment(item.e.Id, item.e.Name, Role.System, item.content));
		}
		if (systemParts.Count > 0)
		{
			result.Add((PromptRole.System, string.Join("\n\n", systemParts.Select(x => x.content))));
		}
		int systemBoundary = result.Count;
		lastHistoryIndex = result.Count;
		foreach (PromptEntry entry in preset.Entries.Where((PromptEntry e) => e.Enabled && e.Role != PromptRole.System && e.Position == PromptPosition.Relative))
		{
			if (entry.IsMainChatHistory)
			{
				if (context.ChatHistory != null)
				{
					foreach (var item2 in context.ChatHistory)
					{
						Role role = item2.role;
						string message = item2.message;
						PromptRole pRole = (PromptRole)role;
						result.Add((pRole, message));
						segments?.Add(new PromptMessageSegment(entry.Id, entry.Name ?? "History", role, message));
					}
				}
				lastHistoryIndex = result.Count;
			}
			else
			{
				string content = ScribanParser.Render(entry.Content, context);
				if (!string.IsNullOrWhiteSpace(content))
				{
					result.Add((entry.Role, content));
					segments?.Add(new PromptMessageSegment(entry.Id, entry.Name ?? "Entry", (Role)entry.Role, content));
				}
			}
		}
		foreach (PromptEntry entry2 in preset.GetInChatEntries())
		{
			string content2 = ScribanParser.Render(entry2.Content, context);
			if (!string.IsNullOrWhiteSpace(content2))
			{
				int insertIndex = Math.Max(systemBoundary, lastHistoryIndex - entry2.InChatDepth);
				result.Insert(insertIndex, (entry2.Role, content2));
				segments?.Insert(insertIndex, new PromptMessageSegment(entry2.Id, entry2.Name ?? "Entry", (Role)entry2.Role, content2));
				if (insertIndex <= lastHistoryIndex)
				{
					lastHistoryIndex++;
				}
				systemBoundary++;
			}
		}
		return MergeConsecutiveRoles(result);
	}
}
