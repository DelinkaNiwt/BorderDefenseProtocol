using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimTalk.Prompt;
using RimTalk.Util;
using Verse;

namespace RimTalk.API;

public static class RimTalkPromptAPI
{
	public static void RegisterPawnVariable(string modId, string variableName, Func<Pawn, string> provider, string description = null, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(variableName) || provider == null)
		{
			Logger.Warning("RimTalkPromptAPI.RegisterPawnVariable: Invalid parameters");
			return;
		}
		ContextHookRegistry.RegisterPawnVariable(variableName, SanitizeModId(modId), provider, description, priority);
		Logger.Debug("Mod '" + modId + "' registered pawn variable: {{pawnN." + variableName + "}}");
	}

	public static void RegisterEnvironmentVariable(string modId, string variableName, Func<Map, string> provider, string description = null, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(variableName) || provider == null)
		{
			Logger.Warning("RimTalkPromptAPI.RegisterEnvironmentVariable: Invalid parameters");
			return;
		}
		ContextHookRegistry.RegisterEnvironmentVariable(variableName, SanitizeModId(modId), provider, description, priority);
		Logger.Debug("Mod '" + modId + "' registered environment variable: {{" + variableName + "}}");
	}

	public static void RegisterContextVariable(string modId, string variableName, Func<PromptContext, string> provider, string description = null, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(variableName) || provider == null)
		{
			Logger.Warning("RimTalkPromptAPI.RegisterContextVariable: Invalid parameters");
			return;
		}
		ContextHookRegistry.RegisterContextVariable(variableName, SanitizeModId(modId), provider, description, priority);
		Logger.Debug("Mod '" + modId + "' registered context variable: {{" + variableName + "}}");
	}

	public static IEnumerable<(string Name, string ModId, string Description, string Type)> GetRegisteredCustomVariables()
	{
		return ContextHookRegistry.GetAllCustomVariables();
	}

	public static bool AddPromptEntry(PromptEntry entry)
	{
		if (entry == null)
		{
			return false;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			Logger.Warning("RimTalkPromptAPI.AddPromptEntry: No active preset");
			return false;
		}
		preset.AddEntry(entry);
		Logger.Debug("Added prompt entry: " + entry.Name);
		return true;
	}

	public static bool InsertPromptEntry(PromptEntry entry, int index)
	{
		if (entry == null)
		{
			return false;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			Logger.Warning("RimTalkPromptAPI.InsertPromptEntry: No active preset");
			return false;
		}
		preset.InsertEntry(entry, index);
		Logger.Debug($"Inserted prompt entry: {entry.Name} at index {index}");
		return true;
	}

	public static bool InsertPromptEntryAfter(PromptEntry entry, string afterEntryId)
	{
		if (entry == null)
		{
			return false;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			Logger.Warning("RimTalkPromptAPI.InsertPromptEntryAfter: No active preset");
			return false;
		}
		bool result = preset.InsertEntryAfter(entry, afterEntryId);
		Logger.Debug($"Inserted prompt entry: {entry.Name} after {afterEntryId} (found: {result})");
		return result;
	}

	public static bool InsertPromptEntryBefore(PromptEntry entry, string beforeEntryId)
	{
		if (entry == null)
		{
			return false;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			Logger.Warning("RimTalkPromptAPI.InsertPromptEntryBefore: No active preset");
			return false;
		}
		bool result = preset.InsertEntryBefore(entry, beforeEntryId);
		Logger.Debug($"Inserted prompt entry: {entry.Name} before {beforeEntryId} (found: {result})");
		return result;
	}

	public static bool InsertPromptEntryAfterName(PromptEntry entry, string afterEntryName)
	{
		if (entry == null || string.IsNullOrEmpty(afterEntryName))
		{
			return false;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			Logger.Warning("RimTalkPromptAPI.InsertPromptEntryAfterName: No active preset");
			return false;
		}
		string targetId = preset.FindEntryIdByName(afterEntryName);
		if (targetId == null)
		{
			preset.AddEntry(entry);
			Logger.Debug("Inserted prompt entry: " + entry.Name + " (target '" + afterEntryName + "' not found, added at end)");
			return false;
		}
		return InsertPromptEntryAfter(entry, targetId);
	}

	public static bool InsertPromptEntryBeforeName(PromptEntry entry, string beforeEntryName)
	{
		if (entry == null || string.IsNullOrEmpty(beforeEntryName))
		{
			return false;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			Logger.Warning("RimTalkPromptAPI.InsertPromptEntryBeforeName: No active preset");
			return false;
		}
		string targetId = preset.FindEntryIdByName(beforeEntryName);
		if (targetId == null)
		{
			preset.AddEntry(entry);
			Logger.Debug("Inserted prompt entry: " + entry.Name + " (target '" + beforeEntryName + "' not found, added at end)");
			return false;
		}
		return InsertPromptEntryBefore(entry, targetId);
	}

	public static string FindEntryIdByName(string entryName)
	{
		if (string.IsNullOrEmpty(entryName))
		{
			return null;
		}
		return PromptManager.Instance.GetActivePreset()?.FindEntryIdByName(entryName);
	}

	public static bool RemovePromptEntry(string entryId)
	{
		if (string.IsNullOrEmpty(entryId))
		{
			return false;
		}
		return PromptManager.Instance.GetActivePreset()?.RemoveEntry(entryId) ?? false;
	}

	public static int RemovePromptEntriesByModId(string modId)
	{
		if (string.IsNullOrEmpty(modId))
		{
			return 0;
		}
		PromptPreset preset = PromptManager.Instance.GetActivePreset();
		if (preset == null)
		{
			return 0;
		}
		List<PromptEntry> toRemove = preset.Entries.Where((PromptEntry e) => e.SourceModId == modId).ToList();
		foreach (PromptEntry entry in toRemove)
		{
			preset.Entries.Remove(entry);
		}
		return toRemove.Count;
	}

	public static VariableStore GetVariableStore()
	{
		return PromptManager.Instance.VariableStore;
	}

	public static void SetGlobalVariable(string key, string value)
	{
		PromptManager.Instance.VariableStore.SetVar(key, value);
	}

	public static string GetGlobalVariable(string key, string defaultValue = "")
	{
		return PromptManager.Instance.VariableStore.GetVar(key, defaultValue);
	}

	public static PromptPreset GetActivePreset()
	{
		return PromptManager.Instance.GetActivePreset();
	}

	public static IReadOnlyList<PromptPreset> GetAllPresets()
	{
		return PromptManager.Instance.Presets;
	}

	public static void RegisterPawnHook(string modId, ContextCategory category, ContextHookRegistry.HookOperation operation, Func<Pawn, string, string> handler, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || handler == null)
		{
			Logger.Warning("RimTalkPromptAPI.RegisterPawnHook: Invalid parameters");
			return;
		}
		ContextHookRegistry.RegisterPawnHook(category, operation, SanitizeModId(modId), handler, priority);
		Logger.Debug($"Mod '{modId}' registered {operation} hook for {category}");
	}

	public static void RegisterEnvironmentHook(string modId, ContextCategory category, ContextHookRegistry.HookOperation operation, Func<Map, string, string> handler, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || handler == null)
		{
			Logger.Warning("RimTalkPromptAPI.RegisterEnvironmentHook: Invalid parameters");
			return;
		}
		ContextHookRegistry.RegisterEnvironmentHook(category, operation, SanitizeModId(modId), handler, priority);
		Logger.Debug($"Mod '{modId}' registered {operation} hook for {category}");
	}

	public static void InjectPawnSection(string modId, string sectionName, ContextCategory anchor, ContextHookRegistry.InjectPosition position, Func<Pawn, string> provider, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(sectionName) || provider == null)
		{
			Logger.Warning("RimTalkPromptAPI.InjectPawnSection: Invalid parameters");
			return;
		}
		ContextHookRegistry.InjectPawnSection(sectionName, SanitizeModId(modId), anchor, position, provider, priority);
		Logger.Debug($"Mod '{modId}' injected pawn section '{sectionName}' {position} {anchor}");
	}

	public static void InjectEnvironmentSection(string modId, string sectionName, ContextCategory anchor, ContextHookRegistry.InjectPosition position, Func<Map, string> provider, int priority = 100)
	{
		if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(sectionName) || provider == null)
		{
			Logger.Warning("RimTalkPromptAPI.InjectEnvironmentSection: Invalid parameters");
			return;
		}
		ContextHookRegistry.InjectEnvironmentSection(sectionName, SanitizeModId(modId), anchor, position, provider, priority);
		Logger.Debug($"Mod '{modId}' injected environment section '{sectionName}' {position} {anchor}");
	}

	public static void UnregisterAllHooks(string modId)
	{
		if (!string.IsNullOrEmpty(modId))
		{
			ContextHookRegistry.UnregisterMod(SanitizeModId(modId));
			Logger.Debug("Mod '" + modId + "' unregistered all hooks");
		}
	}

	public static bool HasAnyHooks()
	{
		return ContextHookRegistry.HasAnyHooks || ContextHookRegistry.HasAnyInjections;
	}

	public static PromptEntry CreatePromptEntry(string name, string content, PromptRole role = PromptRole.System, PromptPosition position = PromptPosition.Relative, int inChatDepth = 0, string sourceModId = null)
	{
		return new PromptEntry
		{
			Name = name,
			Content = content,
			Role = role,
			Position = position,
			InChatDepth = inChatDepth,
			SourceModId = sourceModId,
			Enabled = true
		};
	}

	private static string SanitizeModId(string modId)
	{
		string sanitized = Regex.Replace(modId.ToLowerInvariant(), "[^a-z0-9]", "");
		return string.IsNullOrEmpty(sanitized) ? "unknown" : sanitized;
	}
}
