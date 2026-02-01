using System;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Util;
using Verse;

namespace RimTalk.API;

public static class ContextHookRegistry
{
	public enum HookOperation
	{
		Append,
		Prepend,
		Override
	}

	public enum InjectPosition
	{
		Before,
		After
	}

	private class HookEntry
	{
		public string ModId { get; }

		public Delegate Handler { get; }

		public int Priority { get; }

		public HookEntry(string modId, Delegate handler, int priority)
		{
			ModId = modId;
			Handler = handler;
			Priority = priority;
		}
	}

	private class InjectedSection
	{
		public string Name { get; }

		public string ModId { get; }

		public ContextCategory Anchor { get; }

		public InjectPosition Position { get; }

		public int Priority { get; }

		public Delegate Provider { get; }

		public InjectedSection(string name, string modId, ContextCategory anchor, InjectPosition position, int priority, Delegate provider)
		{
			Name = name;
			ModId = modId;
			Anchor = anchor;
			Position = position;
			Priority = priority;
			Provider = provider;
		}
	}

	private class CustomVariableEntry
	{
		public string Name { get; }

		public string ModId { get; }

		public string Description { get; }

		public Delegate Provider { get; }

		public int Priority { get; }

		public CustomVariableEntry(string name, string modId, string description, Delegate provider, int priority)
		{
			Name = name;
			ModId = modId;
			Description = description;
			Provider = provider;
			Priority = priority;
		}
	}

	private static readonly Dictionary<ContextCategory, List<HookEntry>> PrependHooks = new Dictionary<ContextCategory, List<HookEntry>>(ContextCategory.Comparer);

	private static readonly Dictionary<ContextCategory, List<HookEntry>> AppendHooks = new Dictionary<ContextCategory, List<HookEntry>>(ContextCategory.Comparer);

	private static readonly Dictionary<ContextCategory, List<HookEntry>> OverrideHooks = new Dictionary<ContextCategory, List<HookEntry>>(ContextCategory.Comparer);

	private static readonly List<InjectedSection> InjectedSections = new List<InjectedSection>();

	private static readonly Dictionary<string, CustomVariableEntry> CustomPawnVariables = new Dictionary<string, CustomVariableEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CustomVariableEntry> CustomEnvironmentVariables = new Dictionary<string, CustomVariableEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CustomVariableEntry> CustomContextVariables = new Dictionary<string, CustomVariableEntry>(StringComparer.OrdinalIgnoreCase);

	public static bool HasAnyHooks => PrependHooks.Count > 0 || AppendHooks.Count > 0 || OverrideHooks.Count > 0;

	public static bool HasAnyInjections => InjectedSections.Count > 0;

	public static bool HasAnyCustomVariables => CustomPawnVariables.Count > 0 || CustomEnvironmentVariables.Count > 0 || CustomContextVariables.Count > 0;

	public static void RegisterPawnVariable(string variableName, string modId, Func<Pawn, string> provider, string description = null, int priority = 100)
	{
		if (!string.IsNullOrEmpty(variableName) && provider != null)
		{
			string key = variableName.ToLowerInvariant();
			CustomPawnVariables[key] = new CustomVariableEntry(variableName, modId, description, provider, priority);
			Logger.Debug("Registered pawn variable '" + variableName + "' by " + modId);
		}
	}

	public static void RegisterEnvironmentVariable(string variableName, string modId, Func<Map, string> provider, string description = null, int priority = 100)
	{
		if (!string.IsNullOrEmpty(variableName) && provider != null)
		{
			string key = variableName.ToLowerInvariant();
			CustomEnvironmentVariables[key] = new CustomVariableEntry(variableName, modId, description, provider, priority);
			Logger.Debug("Registered environment variable '" + variableName + "' by " + modId);
		}
	}

	public static void RegisterContextVariable(string variableName, string modId, Delegate provider, string description = null, int priority = 100)
	{
		if (!string.IsNullOrEmpty(variableName) && (object)provider != null)
		{
			string key = variableName.ToLowerInvariant();
			CustomContextVariables[key] = new CustomVariableEntry(variableName, modId, description, provider, priority);
			Logger.Debug("Registered context variable '" + variableName + "' by " + modId);
		}
	}

	public static bool TryGetPawnVariable(string variableName, Pawn pawn, out string value)
	{
		value = null;
		if (string.IsNullOrEmpty(variableName) || pawn == null)
		{
			return false;
		}
		string key = variableName.ToLowerInvariant();
		if (!CustomPawnVariables.TryGetValue(key, out var entry))
		{
			return false;
		}
		try
		{
			if (entry.Provider is Func<Pawn, string> provider)
			{
				value = provider(pawn) ?? "";
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Warning("Custom pawn variable '" + variableName + "' from " + entry.ModId + " failed: " + ex.Message);
		}
		return false;
	}

	public static bool TryGetEnvironmentVariable(string variableName, Map map, out string value)
	{
		value = null;
		if (string.IsNullOrEmpty(variableName) || map == null)
		{
			return false;
		}
		string key = variableName.ToLowerInvariant();
		if (!CustomEnvironmentVariables.TryGetValue(key, out var entry))
		{
			return false;
		}
		try
		{
			if (entry.Provider is Func<Map, string> provider)
			{
				value = provider(map) ?? "";
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Warning("Custom environment variable '" + variableName + "' from " + entry.ModId + " failed: " + ex.Message);
		}
		return false;
	}

	public static bool TryGetContextVariable(string variableName, object context, out string value)
	{
		value = null;
		if (string.IsNullOrEmpty(variableName))
		{
			return false;
		}
		string key = variableName.ToLowerInvariant();
		if (!CustomContextVariables.TryGetValue(key, out var entry))
		{
			return false;
		}
		try
		{
			value = entry.Provider.DynamicInvoke(context)?.ToString() ?? "";
			return true;
		}
		catch (Exception ex)
		{
			Logger.Warning("Custom context variable '" + variableName + "' from " + entry.ModId + " failed: " + ex.Message);
		}
		return false;
	}

	public static IEnumerable<(string Name, string ModId, string Description, string Type)> GetAllCustomVariables()
	{
		foreach (CustomVariableEntry entry in CustomPawnVariables.Values.OrderBy((CustomVariableEntry e) => e.Priority))
		{
			yield return (Name: "pawnN." + entry.Name, ModId: entry.ModId, Description: entry.Description ?? "", Type: "Pawn");
		}
		foreach (CustomVariableEntry entry2 in CustomEnvironmentVariables.Values.OrderBy((CustomVariableEntry e) => e.Priority))
		{
			yield return (Name: entry2.Name, ModId: entry2.ModId, Description: entry2.Description ?? "", Type: "Environment");
		}
		foreach (CustomVariableEntry entry3 in CustomContextVariables.Values.OrderBy((CustomVariableEntry e) => e.Priority))
		{
			yield return (Name: entry3.Name, ModId: entry3.ModId, Description: entry3.Description ?? "", Type: "Context");
		}
	}

	public static bool HasPawnVariable(string variableName)
	{
		return !string.IsNullOrEmpty(variableName) && CustomPawnVariables.ContainsKey(variableName.ToLowerInvariant());
	}

	public static bool HasEnvironmentVariable(string variableName)
	{
		return !string.IsNullOrEmpty(variableName) && CustomEnvironmentVariables.ContainsKey(variableName.ToLowerInvariant());
	}

	public static bool HasContextVariable(string variableName)
	{
		return !string.IsNullOrEmpty(variableName) && CustomContextVariables.ContainsKey(variableName.ToLowerInvariant());
	}

	public static void RegisterPawnHook(ContextCategory category, HookOperation operation, string modId, Func<Pawn, string, string> handler, int priority = 100)
	{
		if (category.Type != ContextType.Pawn)
		{
			Logger.Warning($"RegisterPawnHook: Category '{category}' is not a Pawn category");
		}
		else
		{
			RegisterHookInternal(category, operation, modId, handler, priority);
		}
	}

	public static void InjectPawnSection(string sectionName, string modId, ContextCategory anchor, InjectPosition position, Func<Pawn, string> provider, int priority = 100)
	{
		if (anchor.Type != ContextType.Pawn)
		{
			Logger.Warning($"InjectPawnSection: Anchor '{anchor}' must be a Pawn category");
		}
		else
		{
			InjectSectionInternal(sectionName, modId, anchor, position, provider, priority);
		}
	}

	public static void RegisterEnvironmentHook(ContextCategory category, HookOperation operation, string modId, Func<Map, string, string> handler, int priority = 100)
	{
		if (category.Type != ContextType.Environment)
		{
			Logger.Warning($"RegisterEnvironmentHook: Category '{category}' is not an Environment category");
		}
		else
		{
			RegisterHookInternal(category, operation, modId, handler, priority);
		}
	}

	public static void InjectEnvironmentSection(string sectionName, string modId, ContextCategory anchor, InjectPosition position, Func<Map, string> provider, int priority = 100)
	{
		if (anchor.Type != ContextType.Environment)
		{
			Logger.Warning($"InjectEnvironmentSection: Anchor '{anchor}' must be an Environment category");
		}
		else
		{
			InjectSectionInternal(sectionName, modId, anchor, position, provider, priority);
		}
	}

	private static void RegisterHookInternal(ContextCategory category, HookOperation operation, string modId, Delegate handler, int priority)
	{
		HookEntry entry = new HookEntry(modId, handler, priority);
		if (1 == 0)
		{
		}
		Dictionary<ContextCategory, List<HookEntry>> dictionary = operation switch
		{
			HookOperation.Prepend => PrependHooks, 
			HookOperation.Append => AppendHooks, 
			HookOperation.Override => OverrideHooks, 
			_ => throw new ArgumentOutOfRangeException("operation"), 
		};
		if (1 == 0)
		{
		}
		Dictionary<ContextCategory, List<HookEntry>> targetDict = dictionary;
		if (!targetDict.TryGetValue(category, out var list))
		{
			list = (targetDict[category] = new List<HookEntry>());
		}
		list.Add(entry);
		list.Sort((HookEntry a, HookEntry b) => a.Priority.CompareTo(b.Priority));
		Logger.Debug($"Registered {operation} hook for {category} by {modId} (priority {priority})");
	}

	private static void InjectSectionInternal(string sectionName, string modId, ContextCategory anchor, InjectPosition position, Delegate provider, int priority)
	{
		InjectedSections.Add(new InjectedSection(sectionName, modId, anchor, position, priority, provider));
		InjectedSections.Sort(delegate(InjectedSection a, InjectedSection b)
		{
			int num = string.Compare(a.Anchor.Key, b.Anchor.Key, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			int num2 = a.Position.CompareTo(b.Position);
			return (num2 != 0) ? num2 : a.Priority.CompareTo(b.Priority);
		});
		Logger.Debug($"Injected section '{sectionName}' {position} {anchor} (priority {priority}) by {modId}");
	}

	public static string ApplyPawnHooks(ContextCategory category, Pawn pawn, string originalValue)
	{
		if (OverrideHooks.TryGetValue(category, out var overrideList))
		{
			foreach (HookEntry hook in overrideList)
			{
				try
				{
					if (hook.Handler is Func<Pawn, string, string> h)
					{
						string result = h(pawn, originalValue);
						if (result != null)
						{
							return result;
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Warning($"Override hook from '{hook.ModId}' for {category} failed: {ex.Message}");
				}
			}
		}
		string value = originalValue;
		if (PrependHooks.TryGetValue(category, out var prependList))
		{
			foreach (HookEntry hook2 in prependList)
			{
				try
				{
					if (hook2.Handler is Func<Pawn, string, string> h2)
					{
						value = h2(pawn, value) ?? value;
					}
				}
				catch (Exception ex2)
				{
					Logger.Warning($"Prepend hook from '{hook2.ModId}' for {category} failed: {ex2.Message}");
				}
			}
		}
		if (AppendHooks.TryGetValue(category, out var appendList))
		{
			foreach (HookEntry hook3 in appendList)
			{
				try
				{
					if (hook3.Handler is Func<Pawn, string, string> h3)
					{
						value = h3(pawn, value) ?? value;
					}
				}
				catch (Exception ex3)
				{
					Logger.Warning($"Append hook from '{hook3.ModId}' for {category} failed: {ex3.Message}");
				}
			}
		}
		return value;
	}

	public static string ApplyEnvironmentHooks(ContextCategory category, Map map, string originalValue)
	{
		if (OverrideHooks.TryGetValue(category, out var overrideList))
		{
			foreach (HookEntry hook in overrideList)
			{
				try
				{
					if (hook.Handler is Func<Map, string, string> h)
					{
						string result = h(map, originalValue);
						if (result != null)
						{
							return result;
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Warning($"Override hook from '{hook.ModId}' for {category} failed: {ex.Message}");
				}
			}
		}
		string value = originalValue;
		if (PrependHooks.TryGetValue(category, out var prependList))
		{
			foreach (HookEntry hook2 in prependList)
			{
				try
				{
					if (hook2.Handler is Func<Map, string, string> h2)
					{
						value = h2(map, value) ?? value;
					}
				}
				catch (Exception ex2)
				{
					Logger.Warning($"Prepend hook from '{hook2.ModId}' for {category} failed: {ex2.Message}");
				}
			}
		}
		if (AppendHooks.TryGetValue(category, out var appendList))
		{
			foreach (HookEntry hook3 in appendList)
			{
				try
				{
					if (hook3.Handler is Func<Map, string, string> h3)
					{
						value = h3(map, value) ?? value;
					}
				}
				catch (Exception ex3)
				{
					Logger.Warning($"Append hook from '{hook3.ModId}' for {category} failed: {ex3.Message}");
				}
			}
		}
		return value;
	}

	public static IEnumerable<(string Name, InjectPosition Position, int Priority, Delegate Provider)> GetInjectedSectionsAt(ContextCategory anchor)
	{
		return from s in InjectedSections
			where s.Anchor.Equals(anchor)
			orderby s.Position, s.Priority
			select (Name: s.Name, Position: s.Position, Priority: s.Priority, Provider: s.Provider);
	}

	public static Func<Pawn, string> GetInjectedPawnSection(string name)
	{
		return InjectedSections.FirstOrDefault((InjectedSection s) => s.Anchor.Type == ContextType.Pawn && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))?.Provider as Func<Pawn, string>;
	}

	public static Func<Map, string> GetInjectedEnvironmentSection(string name)
	{
		return InjectedSections.FirstOrDefault((InjectedSection s) => s.Anchor.Type == ContextType.Environment && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))?.Provider as Func<Map, string>;
	}

	public static void UnregisterMod(string modId)
	{
		RemoveFromDict(PrependHooks, modId);
		RemoveFromDict(AppendHooks, modId);
		RemoveFromDict(OverrideHooks, modId);
		InjectedSections.RemoveAll((InjectedSection s) => s.ModId == modId);
		RemoveCustomVariables(CustomPawnVariables, modId);
		RemoveCustomVariables(CustomEnvironmentVariables, modId);
		RemoveCustomVariables(CustomContextVariables, modId);
		Logger.Debug("Unregistered all hooks and variables from mod: " + modId);
	}

	private static void RemoveCustomVariables(Dictionary<string, CustomVariableEntry> dict, string modId)
	{
		List<string> keysToRemove = (from kvp in dict
			where kvp.Value.ModId == modId
			select kvp.Key).ToList();
		foreach (string key in keysToRemove)
		{
			dict.Remove(key);
		}
	}

	private static void RemoveFromDict(Dictionary<ContextCategory, List<HookEntry>> dict, string modId)
	{
		foreach (ContextCategory key in dict.Keys.ToList())
		{
			dict[key].RemoveAll((HookEntry e) => e.ModId == modId);
			if (dict[key].Count == 0)
			{
				dict.Remove(key);
			}
		}
	}

	public static void Clear()
	{
		PrependHooks.Clear();
		AppendHooks.Clear();
		OverrideHooks.Clear();
		InjectedSections.Clear();
		CustomPawnVariables.Clear();
		CustomEnvironmentVariables.Clear();
		CustomContextVariables.Clear();
		Logger.Debug("ContextHookRegistry cleared all registrations");
	}
}
