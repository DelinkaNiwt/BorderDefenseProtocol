using System;
using System.Collections.Generic;
using System.Linq;

namespace NCL;

public static class DictionaryExtensions
{
	public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<KeyValuePair<TKey, TValue>, bool> predicate)
	{
		foreach (KeyValuePair<TKey, TValue> item in dict.Where(predicate).ToList())
		{
			dict.Remove(item.Key);
		}
	}
}
