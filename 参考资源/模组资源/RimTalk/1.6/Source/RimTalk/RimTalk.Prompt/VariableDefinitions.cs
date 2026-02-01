using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RimTalk.API;
using RimTalk.Util;
using RimWorld;
using Scriban.Runtime;
using Verse;

namespace RimTalk.Prompt;

public static class VariableDefinitions
{
	private static readonly Dictionary<string, Type> RootTypeMap;

	private static readonly HashSet<string> StaticRoots;

	static VariableDefinitions()
	{
		RootTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		StaticRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		RootTypeMap["pawn"] = typeof(Pawn);
		RootTypeMap["recipient"] = typeof(Pawn);
		RootTypeMap["pawns"] = typeof(List<Pawn>);
		RootTypeMap["map"] = typeof(Map);
		RootTypeMap["ctx"] = typeof(PromptContext);
		RootTypeMap["PawnsFinder"] = typeof(PawnsFinder);
		StaticRoots.Add("PawnsFinder");
		RootTypeMap["Find"] = typeof(Find);
		StaticRoots.Add("Find");
		RootTypeMap["GenDate"] = typeof(GenDate);
		StaticRoots.Add("GenDate");
		RootTypeMap["lang"] = typeof(string);
		RootTypeMap["prompt"] = typeof(string);
		RootTypeMap["context"] = typeof(string);
		RootTypeMap["json"] = typeof(ScriptObject);
		RootTypeMap["chat"] = typeof(ScriptObject);
		PropertyInfo[] properties = typeof(PromptContext).GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo prop in properties)
		{
			if (!RootTypeMap.ContainsKey(prop.Name))
			{
				RootTypeMap[prop.Name] = prop.PropertyType;
			}
		}
	}

	public static Dictionary<string, List<(string name, string description)>> GetScribanVariables()
	{
		Dictionary<string, List<(string, string)>> dict = new Dictionary<string, List<(string, string)>>();
		List<(string, string)> pawnMagic = ContextCategories.Pawn.All.Select((ContextCategory c) => ("pawn." + c.Key, c.Key.CapitalizeFirst())).ToList();
		dict["RimTalk.ScribanVar.Category.PawnShorthands".Translate()] = pawnMagic;
		AddStaticMethods(dict, "Utility: Pawn", typeof(PawnUtil));
		AddStaticMethods(dict, "Utility: Common", typeof(CommonUtil));
		dict["RimTalk.ScribanVar.Category.CoreObjects".Translate()] = new List<(string, string)>
		{
			("pawn", "The primary character (initiator)"),
			("recipient", "The character being spoken to (if any)"),
			("pawns", "List of all pawns in the dialogue"),
			("map", "The current map object"),
			("ctx", "The full prompt context object")
		};
		dict["RimTalk.ScribanVar.Category.Context".Translate()] = new List<(string, string)>
		{
			("prompt", "Full decorated prompt including time, weather, and location"),
			("context", "Raw formatted string describing the initiator's details"),
			("ctx.DialogueType", "Type of dialogue (monologue, conversation, etc.)"),
			("ctx.DialogueStatus", "Current status of the dialogue"),
			("ctx.PawnContext", "Formatted string describing the pawn"),
			("ctx.UserPrompt", "The raw prompt from the user (if any)"),
			("ctx.IsMonologue", "True if this is a monologue")
		};
		dict["RimTalk.ScribanVar.Category.GameStatic".Translate()] = new List<(string, string)>
		{
			("PawnsFinder", "Access to global pawn lists"),
			("Find", "Access to current game state (Maps, TickManager)"),
			("GenDate", "Date utilities")
		};
		dict["RimTalk.ScribanVar.Category.System".Translate()] = new List<(string, string)>
		{
			("lang", "Active native language name"),
			("hour", "Current hour (0-23)"),
			("day", "Day of quadrum (1-15)"),
			("quadrum", "Current quadrum (Aprimay, Jugust, Septober, Decembary)"),
			("year", "Current year (e.g. 5500)"),
			("season", "Current season (Spring, Summer, Fall, Winter)"),
			("json.format", "JSON output instructions"),
			("chat.history", "Full conversation history (Role: Message)")
		};
		List<(string, string, string, string)> customVars = ContextHookRegistry.GetAllCustomVariables().ToList();
		if (customVars.Any())
		{
			List<(string, string)> modVarsList = customVars.Select<(string, string, string, string), (string, string)>(((string Name, string ModId, string Description, string Type) v) => (Name: v.Name, "[" + v.Type + "] " + v.Description + " (from " + v.ModId + ")")).ToList();
			dict["RimTalk.Settings.PromptPreset.ModVariables".Translate()] = modVarsList;
		}
		return dict;
	}

	public static Dictionary<string, List<(string name, string description)>> GetDynamicVariables(string query, string fullText = null)
	{
		Dictionary<string, List<(string, string)>> results = new Dictionary<string, List<(string, string)>>();
		if (string.IsNullOrWhiteSpace(query))
		{
			return results;
		}
		var (currentType, isStaticContext, currentPath) = ResolveTypePath(query, fullText);
		if (currentType == null)
		{
			return results;
		}
		string filter = "";
		int lastDotIndex = query.LastIndexOf('.');
		if (lastDotIndex >= 0 && lastDotIndex < query.Length - 1)
		{
			filter = query.Substring(lastDotIndex + 1);
		}
		else if (lastDotIndex < 0 && !query.Contains("["))
		{
			filter = query.Trim();
		}
		List<(string, string)> candidates = new List<(string, string)>();
		BindingFlags suggestionFlags = (BindingFlags)(0x10 | (isStaticContext ? 8 : 4));
		PropertyInfo[] properties = currentType.GetProperties(suggestionFlags);
		foreach (PropertyInfo prop in properties)
		{
			if (prop.GetIndexParameters().Length == 0 && !prop.IsDefined(typeof(ObsoleteAttribute), inherit: true) && (string.IsNullOrEmpty(filter) || prop.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				candidates.Add((currentPath + "." + prop.Name, prop.PropertyType.Name ?? ""));
			}
		}
		FieldInfo[] fields = currentType.GetFields(suggestionFlags);
		foreach (FieldInfo field in fields)
		{
			if (!field.IsDefined(typeof(ObsoleteAttribute), inherit: true) && (string.IsNullOrEmpty(filter) || field.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				candidates.Add((currentPath + "." + field.Name, field.FieldType.Name ?? ""));
			}
		}
		MethodInfo[] methods = currentType.GetMethods(suggestionFlags);
		foreach (MethodInfo method in methods)
		{
			if (!method.IsSpecialName && !(method.DeclaringType == typeof(object)) && !method.IsDefined(typeof(ObsoleteAttribute), inherit: true) && !(method.ReturnType == typeof(void)) && (string.IsNullOrEmpty(filter) || method.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				string paramList = string.Join(", ", from p in method.GetParameters()
					select p.ParameterType.Name);
				candidates.Add((currentPath + "." + method.Name, method.ReturnType.Name + " (" + paramList + ")"));
			}
		}
		if (!isStaticContext)
		{
			Type[] utils = new Type[3]
			{
				typeof(PawnUtil),
				typeof(CommonUtil),
				typeof(GenderUtility)
			};
			Type[] array = utils;
			foreach (Type util in array)
			{
				IEnumerable<MethodInfo> extensions = from m in util.GetMethods(BindingFlags.Static | BindingFlags.Public)
					where m.IsDefined(typeof(ExtensionAttribute), inherit: false) && m.GetParameters().Length != 0 && m.GetParameters()[0].ParameterType.IsAssignableFrom(currentType) && m.ReturnType != typeof(void)
					select m;
				foreach (MethodInfo method2 in extensions)
				{
					if (string.IsNullOrEmpty(filter) || method2.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						string paramList2 = string.Join(", ", from p in method2.GetParameters().Skip(1)
							select p.ParameterType.Name);
						candidates.Add((currentPath + "." + method2.Name, method2.ReturnType.Name + " (" + paramList2 + ")"));
					}
				}
			}
		}
		if (candidates.Any())
		{
			results["Dynamic: " + currentType.Name] = candidates.OrderBy(((string, string) x) => x.Item1).ToList();
		}
		if (typeof(Pawn).IsAssignableFrom(currentType))
		{
			AddPawnMagicSuggestions(results, currentPath, filter);
		}
		else if (typeof(Map).IsAssignableFrom(currentType))
		{
			AddMapMagicSuggestions(results, currentPath, filter);
		}
		return results;
	}

	private static void AddPawnMagicSuggestions(Dictionary<string, List<(string name, string description)>> results, string path, string filter)
	{
		List<(string, string)> magic = new List<(string, string)>();
		foreach (ContextCategory cat in ContextCategories.Pawn.All)
		{
			if (string.IsNullOrEmpty(filter) || cat.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				magic.Add((path + "." + cat.Key, "RimTalk Magic Property"));
			}
		}
		foreach (var v in ContextHookRegistry.GetAllCustomVariables())
		{
			if (v.Type == "Pawn")
			{
				string text;
				if (!v.Name.Contains("."))
				{
					(text, _, _, _) = v;
				}
				else
				{
					text = v.Name.Substring(v.Name.IndexOf('.') + 1);
				}
				string name = text;
				if (string.IsNullOrEmpty(filter) || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					magic.Add((path + "." + name, "Custom (" + v.ModId + ")"));
				}
			}
		}
		if (magic.Any())
		{
			results["AI Context Fields"] = magic.OrderBy(((string, string) x) => x.Item1).ToList();
		}
	}

	private static void AddMapMagicSuggestions(Dictionary<string, List<(string name, string description)>> results, string path, string filter)
	{
		List<(string, string)> magic = new List<(string, string)>();
		foreach (ContextCategory cat in ContextCategories.Environment.All)
		{
			if (string.IsNullOrEmpty(filter) || cat.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				magic.Add((path + "." + cat.Key, "RimTalk Magic Property"));
			}
		}
		foreach (var v in ContextHookRegistry.GetAllCustomVariables())
		{
			if (v.Type == "Environment" && (string.IsNullOrEmpty(filter) || v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				magic.Add((path + "." + v.Name, "Custom (" + v.ModId + ")"));
			}
		}
		if (magic.Any())
		{
			results["Environment Fields"] = magic.OrderBy(((string, string) x) => x.Item1).ToList();
		}
	}

	private static (Type type, bool isStatic, string path) ResolveTypePath(string query, string fullText)
	{
		string cleanQuery = query.Replace("{", "").Replace("}", "").Trim();
		List<string> parts = (from p in Regex.Split(cleanQuery, "(?<=\\.)|(?=\\.)|(?=\\[)|(?<=\\])")
			where p != "." && !string.IsNullOrEmpty(p)
			select p).ToList();
		if (parts.Count == 0)
		{
			return (type: null, isStatic: false, path: "");
		}
		Type currentType = null;
		bool isStaticContext = false;
		string firstPart = parts[0].TrimEnd('.');
		if (RootTypeMap.TryGetValue(firstPart, out var type))
		{
			currentType = type;
			isStaticContext = StaticRoots.Contains(firstPart);
		}
		else if (!string.IsNullOrEmpty(fullText))
		{
			currentType = InferTypeFromText(firstPart, fullText);
		}
		if (currentType == null)
		{
			return (type: null, isStatic: false, path: "");
		}
		string currentPath = firstPart;
		int traversalLimit = ((cleanQuery.EndsWith(".") || cleanQuery.EndsWith("]")) ? parts.Count : (parts.Count - 1));
		for (int i = 1; i < traversalLimit; i++)
		{
			string part = parts[i];
			if (part.StartsWith("["))
			{
				if (currentType.IsArray)
				{
					currentType = currentType.GetElementType();
				}
				else if (currentType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(currentType))
				{
					currentType = currentType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
				}
				else
				{
					PropertyInfo itemProp = currentType.GetProperty("Item");
					if (itemProp != null)
					{
						currentType = itemProp.PropertyType;
					}
				}
				currentPath += part;
				isStaticContext = false;
				continue;
			}
			BindingFlags flags = (BindingFlags)(0x11 | (isStaticContext ? 8 : 4));
			PropertyInfo prop = currentType.GetProperties(flags).FirstOrDefault((PropertyInfo p) => p.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
			FieldInfo field = currentType.GetFields(flags).FirstOrDefault((FieldInfo f) => f.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
			if (prop != null)
			{
				currentType = prop.PropertyType;
				currentPath = currentPath + "." + prop.Name;
				isStaticContext = false;
				continue;
			}
			if (field != null)
			{
				currentType = field.FieldType;
				currentPath = currentPath + "." + field.Name;
				isStaticContext = false;
				continue;
			}
			return (type: null, isStatic: false, path: "");
		}
		return (type: currentType, isStatic: isStaticContext, path: currentPath);
	}

	private static Type InferTypeFromText(string varName, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		Match loopMatch = Regex.Match(text, "for\\s+" + varName + "\\s+in\\s+([a-zA-Z0-9_\\.\\[\\]]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		if (loopMatch.Success)
		{
			string expression = loopMatch.Groups[1].Value;
			Type type = ResolveTypePath(expression + ".", null).type;
			if (type != null)
			{
				if (type.IsArray)
				{
					return type.GetElementType();
				}
				if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
				{
					return type.GetGenericArguments().FirstOrDefault() ?? typeof(object);
				}
				PropertyInfo itemProp = type.GetProperty("Item");
				if (itemProp != null)
				{
					return itemProp.PropertyType;
				}
			}
		}
		Match withMatch = Regex.Match(text, "with\\s+([a-zA-Z0-9_\\.\\[\\]]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		if (withMatch.Success)
		{
			string expression2 = withMatch.Groups[1].Value;
			if (expression2.Equals(varName, StringComparison.OrdinalIgnoreCase))
			{
				return ResolveTypePath(expression2 + ".", null).type;
			}
		}
		Match captureMatch = Regex.Match(text, "capture\\s+" + varName + "\\s*}(.*?){", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		if (captureMatch.Success)
		{
			string content = captureMatch.Groups[1].Value.Trim();
			if (Regex.IsMatch(content, "^[a-zA-Z0-9_\\.]+$"))
			{
				return ResolveTypePath(content + ".", null).type;
			}
		}
		Match assignmentMatch = Regex.Match(text, varName + "\\s*=\\s*([a-zA-Z0-9_\\.\\[\\]]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		if (assignmentMatch.Success)
		{
			string expression3 = assignmentMatch.Groups[1].Value;
			return ResolveTypePath(expression3 + ".", null).type;
		}
		return null;
	}

	private static void AddVanillaProperties(Dictionary<string, List<(string name, string description)>> dict, string prefix, Type type)
	{
		List<(string, string)> list = new List<(string, string)>();
		IOrderedEnumerable<PropertyInfo> props = from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			where p.GetIndexParameters().Length == 0 && !p.IsDefined(typeof(ObsoleteAttribute), inherit: true)
			orderby p.Name
			select p;
		foreach (PropertyInfo prop in props)
		{
			string name = (string.IsNullOrEmpty(prefix) ? prop.Name : (prefix + "." + prop.Name));
			list.Add((name, prop.PropertyType.Name));
		}
		IOrderedEnumerable<FieldInfo> fields = from f in type.GetFields(BindingFlags.Instance | BindingFlags.Public)
			where !f.IsDefined(typeof(ObsoleteAttribute), inherit: true)
			orderby f.Name
			select f;
		foreach (FieldInfo field in fields)
		{
			string name2 = (string.IsNullOrEmpty(prefix) ? field.Name : (prefix + "." + field.Name));
			list.Add((name2, field.FieldType.Name));
		}
		if (list.Any())
		{
			string categoryName = (string.IsNullOrEmpty(prefix) ? "Context (Raw)" : (type.Name + " (Raw)"));
			dict[$"{categoryName} ({list.Count} fields)"] = list;
		}
	}

	private static void AddStaticMethods(Dictionary<string, List<(string name, string description)>> dict, string categoryName, Type type)
	{
		List<(string, string)> list = new List<(string, string)>();
		IOrderedEnumerable<MethodInfo> methods = from m in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
			where !m.IsSpecialName && !m.IsDefined(typeof(ObsoleteAttribute), inherit: true) && m.ReturnType != typeof(void)
			orderby m.Name
			select m;
		foreach (MethodInfo method in methods)
		{
			ParameterInfo[] parameters = method.GetParameters();
			string name = method.Name;
			string paramList = string.Join(", ", parameters.Select((ParameterInfo p) => p.ParameterType.Name));
			list.Add((name, method.ReturnType.Name + " (" + paramList + ")"));
		}
		if (list.Any())
		{
			dict[categoryName] = list;
		}
	}
}
