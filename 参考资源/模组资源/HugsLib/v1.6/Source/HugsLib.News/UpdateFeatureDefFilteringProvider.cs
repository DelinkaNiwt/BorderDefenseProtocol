using System;
using System.Collections.Generic;
using System.Linq;

namespace HugsLib.News;

/// <summary>
/// Filters <see cref="T:HugsLib.UpdateFeatureDef" />s by their mod identifier.
/// </summary>
internal class UpdateFeatureDefFilteringProvider
{
	private struct FilteringEntry
	{
		public string FilterModId { get; }

		public string ModNameReadable { get; }

		public int NewsDefCount { get; }

		public FilteringEntry(string filterModId, string modNameReadable, int newsDefCount)
		{
			FilterModId = filterModId;
			ModNameReadable = modNameReadable;
			NewsDefCount = newsDefCount;
		}
	}

	private readonly FilteringEntry[] filteringEntries;

	private string currentFilterModId;

	public string CurrentFilterModIdentifier
	{
		get
		{
			return currentFilterModId;
		}
		set
		{
			FilteringEntry? filteringEntry = ((value != null) ? TryGetFilteringEntry(value) : ((FilteringEntry?)null));
			currentFilterModId = filteringEntry?.FilterModId;
			CurrentFilterModNameReadable = filteringEntry?.ModNameReadable;
		}
	}

	public string CurrentFilterModNameReadable { get; private set; }

	public UpdateFeatureDefFilteringProvider(IEnumerable<UpdateFeatureDef> newsDefs)
	{
		filteringEntries = GenerateFilteringEntriesFromDefs(newsDefs);
	}

	public IEnumerable<(string id, string label, int defCount)> GetAvailableFilters()
	{
		return filteringEntries.Select((FilteringEntry e) => (FilterModId: e.FilterModId, ModNameReadable: e.ModNameReadable, NewsDefCount: e.NewsDefCount));
	}

	public IEnumerable<UpdateFeatureDef> MatchingDefsOf(IEnumerable<UpdateFeatureDef> defs)
	{
		return (currentFilterModId == null) ? defs : defs.Where((UpdateFeatureDef d) => d?.modIdentifier == currentFilterModId);
	}

	private static FilteringEntry[] GenerateFilteringEntriesFromDefs(IEnumerable<UpdateFeatureDef> defs)
	{
		Dictionary<string, (string, int)> dictionary = new Dictionary<string, (string, int)>();
		foreach (UpdateFeatureDef def in defs)
		{
			string owningModId = def.OwningModId;
			if (owningModId != null)
			{
				if (!dictionary.TryGetValue(owningModId, out var value))
				{
					dictionary.Add(owningModId, (def.modNameReadable, 1));
				}
				else
				{
					dictionary[owningModId] = (value.Item1, value.Item2 + 1);
				}
			}
		}
		return dictionary.Select<KeyValuePair<string, (string, int)>, FilteringEntry>((KeyValuePair<string, (string name, int count)> i) => new FilteringEntry(i.Key, i.Value.name, i.Value.count)).OrderBy((FilteringEntry i) => i.ModNameReadable, StringComparer.InvariantCultureIgnoreCase).ToArray();
	}

	private FilteringEntry? TryGetFilteringEntry(string modIdentifier)
	{
		return filteringEntries.FirstOrDefault((FilteringEntry e) => e.FilterModId == modIdentifier);
	}
}
