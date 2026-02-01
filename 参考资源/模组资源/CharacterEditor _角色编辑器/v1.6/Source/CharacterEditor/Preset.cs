using System;
using System.Collections.Generic;
using Verse;

namespace CharacterEditor;

internal static class Preset
{
	internal static string AsString<T>(SortedDictionary<T, string> dicParams)
	{
		string text = "";
		foreach (T key in dicParams.Keys)
		{
			text = text + dicParams[key] + ",";
		}
		text = text.SubstringRemoveLast();
		return text + ";";
	}

	internal static void ResetAllToDefault<T>(Dictionary<string, T> allDefaults, Action<T> defaultAction, OptionS optionS, string type)
	{
		foreach (T value in allDefaults.Values)
		{
			defaultAction(value);
		}
		CEditor.API.SetCustom(optionS, "", "");
		MessageTool.Show("reset all " + type + " to originals done");
	}

	internal static void ResetToDefault<T>(Dictionary<string, T> allDefaults, Action<T> defaultAction, OptionS optionS, string identifier)
	{
		if (!identifier.NullOrEmpty() && allDefaults.TryGetValue(identifier, out var value))
		{
			defaultAction(value);
			CEditor.API.SetCustom(optionS, "", identifier);
			MessageTool.Show("reset " + identifier + " done");
		}
	}

	internal static void LoadAllModifications(string custom, Action<string> loadAction, string type)
	{
		if (string.IsNullOrEmpty(custom))
		{
			Log.Message("no modifications for " + type);
			return;
		}
		try
		{
			string[] array = custom.Trim().SplitNoEmpty(";");
			string[] array2 = array;
			foreach (string obj in array2)
			{
				loadAction(obj);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal static bool LoadModification<T>(string custom, ref SortedDictionary<T, string> dicParams)
	{
		dicParams = new SortedDictionary<T, string>();
		if (!string.IsNullOrEmpty(custom))
		{
			int num = Enum.GetNames(typeof(T)).EnumerableCount();
			string[] array = custom.SplitNo(",");
			string[] array2 = array;
			foreach (string value in array2)
			{
				if (dicParams.Count < num)
				{
					dicParams.Add((T)Enum.Parse(typeof(T), dicParams.Count.ToString()), value);
				}
			}
		}
		return dicParams.Count != 0;
	}

	internal static void SaveModification<TPreset, TSample>(TSample t, Func<TSample, TPreset> createAction, Action<TPreset> saveAction)
	{
		if (t == null)
		{
			return;
		}
		try
		{
			TPreset obj = createAction(t);
			saveAction(obj);
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
	}

	internal static Dictionary<string, TPreset> CreateDefaults<TPreset, TSample>(HashSet<TSample> list, Func<TSample, string> idGetter, Func<TSample, TPreset> createAction, string type)
	{
		Dictionary<string, TPreset> dictionary = new Dictionary<string, TPreset>();
		foreach (TSample item in list)
		{
			string text = idGetter(item);
			if (text != null)
			{
				dictionary.Add(idGetter(item), createAction(item));
			}
		}
		Log.Message(dictionary.Count + " default entities for " + type + " created");
		return dictionary;
	}
}
