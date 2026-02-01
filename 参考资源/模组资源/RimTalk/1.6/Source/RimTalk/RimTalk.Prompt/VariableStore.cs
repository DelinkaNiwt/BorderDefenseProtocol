using System.Collections.Generic;
using Verse;

namespace RimTalk.Prompt;

public class VariableStore : IExposable
{
	private Dictionary<string, string> _variables = new Dictionary<string, string>();

	public int Count => _variables.Count;

	public void SetVar(string key, string value)
	{
		if (!string.IsNullOrEmpty(key))
		{
			_variables[key.ToLowerInvariant()] = value ?? "";
		}
	}

	public string GetVar(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return "";
		}
		string value;
		return _variables.TryGetValue(key.ToLowerInvariant(), out value) ? value : "";
	}

	public string GetVar(string key, string defaultValue)
	{
		if (string.IsNullOrEmpty(key))
		{
			return defaultValue;
		}
		string value = GetVar(key);
		return string.IsNullOrEmpty(value) ? defaultValue : value;
	}

	public bool HasVar(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		return _variables.ContainsKey(key.ToLowerInvariant());
	}

	public bool RemoveVar(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		return _variables.Remove(key.ToLowerInvariant());
	}

	public void Clear()
	{
		_variables.Clear();
	}

	public IReadOnlyDictionary<string, string> GetAllVariables()
	{
		return _variables;
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref _variables, "variables", LookMode.Value, LookMode.Value);
		if (_variables == null)
		{
			_variables = new Dictionary<string, string>();
		}
	}
}
