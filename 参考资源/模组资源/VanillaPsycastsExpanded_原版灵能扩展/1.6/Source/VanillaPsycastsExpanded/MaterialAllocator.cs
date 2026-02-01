using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

internal static class MaterialAllocator
{
	private struct MaterialInfo
	{
		public string stackTrace;
	}

	private static readonly Dictionary<Material, MaterialInfo> references = new Dictionary<Material, MaterialInfo>();

	public static int nextWarningThreshold;

	private static Dictionary<string, int> snapshot = new Dictionary<string, int>();

	public static Material Create(Material material)
	{
		Material material2 = new Material(material);
		references[material2] = new MaterialInfo
		{
			stackTrace = (Prefs.DevMode ? Environment.StackTrace : "(unavailable)")
		};
		TryReport();
		return material2;
	}

	public static Material Create(Shader shader)
	{
		Material material = new Material(shader);
		references[material] = new MaterialInfo
		{
			stackTrace = (Prefs.DevMode ? Environment.StackTrace : "(unavailable)")
		};
		TryReport();
		return material;
	}

	public static void Destroy(Material material)
	{
		if (!references.ContainsKey(material))
		{
			Log.Error($"Destroying material {material}, but that material was not created through the MaterialTracker");
		}
		references.Remove(material);
		UnityEngine.Object.Destroy(material);
	}

	public static void TryReport()
	{
		if (MaterialWarningThreshold() > nextWarningThreshold)
		{
			nextWarningThreshold = MaterialWarningThreshold();
		}
		if (references.Count > nextWarningThreshold)
		{
			Log.Error($"Material allocator has allocated {references.Count} materials; this may be a sign of a material leak");
			if (Prefs.DevMode)
			{
				MaterialReport();
			}
			nextWarningThreshold *= 2;
		}
	}

	public static int MaterialWarningThreshold()
	{
		return int.MaxValue;
	}

	[DebugOutput("System", false)]
	public static void MaterialReport()
	{
		foreach (string item in (from g in references.GroupBy(delegate(KeyValuePair<Material, MaterialInfo> kvp)
			{
				KeyValuePair<Material, MaterialInfo> keyValuePair = kvp;
				return keyValuePair.Value.stackTrace;
			})
			orderby g.Count() descending
			select $"{g.Count()}: {g.FirstOrDefault().Value.stackTrace}").Take(20))
		{
			Log.Error(item);
		}
	}

	[DebugOutput("System", false)]
	public static void MaterialSnapshot()
	{
		snapshot = new Dictionary<string, int>();
		foreach (IGrouping<string, KeyValuePair<Material, MaterialInfo>> item in references.GroupBy(delegate(KeyValuePair<Material, MaterialInfo> kvp)
		{
			KeyValuePair<Material, MaterialInfo> keyValuePair = kvp;
			return keyValuePair.Value.stackTrace;
		}))
		{
			snapshot[item.Key] = item.Count();
		}
	}

	[DebugOutput("System", false)]
	public static void MaterialDelta()
	{
		IEnumerable<string> source = references.Values.Select((MaterialInfo v) => v.stackTrace).Concat(snapshot.Keys).Distinct();
		Dictionary<string, int> currentSnapshot = new Dictionary<string, int>();
		foreach (IGrouping<string, KeyValuePair<Material, MaterialInfo>> item in references.GroupBy(delegate(KeyValuePair<Material, MaterialInfo> kvp)
		{
			KeyValuePair<Material, MaterialInfo> keyValuePair = kvp;
			return keyValuePair.Value.stackTrace;
		}))
		{
			currentSnapshot[item.Key] = item.Count();
		}
		foreach (string item2 in source.Select((string k) => new KeyValuePair<string, int>(k, currentSnapshot.TryGetValue(k, 0) - snapshot.TryGetValue(k, 0))).OrderByDescending(delegate(KeyValuePair<string, int> kvp)
		{
			KeyValuePair<string, int> keyValuePair = kvp;
			return keyValuePair.Value;
		}).Select(delegate(KeyValuePair<string, int> g)
		{
			KeyValuePair<string, int> keyValuePair = g;
			object arg = keyValuePair.Value;
			keyValuePair = g;
			return $"{arg}: {keyValuePair.Key}";
		})
			.Take(20))
		{
			Log.Error(item2);
		}
	}
}
