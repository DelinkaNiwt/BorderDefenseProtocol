using System.Collections.Generic;
using System.Linq;
using RimTalk.Service;
using RimWorld;
using Verse;

namespace RimTalk.Util;

public static class ContextHelper
{
	public static string GetPawnLocationStatus(Pawn pawn)
	{
		if (pawn?.Map == null || pawn.Position == IntVec3.Invalid)
		{
			return null;
		}
		Room room = pawn.GetRoom();
		return (room != null && !room.PsychologicallyOutdoors) ? "Indoors".Translate() : "Outdoors".Translate();
	}

	public static Dictionary<Thought, float> GetThoughts(Pawn pawn)
	{
		List<Thought> thoughts = new List<Thought>();
		pawn?.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughts);
		return (from t in thoughts
			group t by t.def.defName).ToDictionary((IGrouping<string, Thought> g) => g.First(), (IGrouping<string, Thought> g) => g.Sum((Thought t) => t.MoodOffset()));
	}

	public static string GetDecoratedName(Pawn pawn)
	{
		if (!pawn.RaceProps.Humanlike)
		{
			return $"{pawn.LabelShort}({pawn.ageTracker.AgeBiologicalYears}/{pawn.def.LabelCap})";
		}
		string race = ((ModsConfig.BiotechActive && pawn.genes?.Xenotype != null) ? pawn.genes.XenotypeLabel : pawn.def.LabelCap.RawText);
		return $"{pawn.LabelShort}({pawn.ageTracker.AgeBiologicalYears}{pawn.gender.GetLabelShort()}/{pawn.GetRole(includeFaction: true)}/{race})";
	}

	public static bool IsWall(Thing thing)
	{
		GraphicData data = thing.def.graphicData;
		return data != null && data.linkFlags.HasFlag(LinkFlags.Wall);
	}

	public static string FormatBackstory(string label, BackstoryDef backstory, Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		string result = label + ": " + backstory.title + "(" + backstory.titleShort + ")";
		if (infoLevel == PromptService.InfoLevel.Full)
		{
			result = result + ":" + CommonUtil.Sanitize(backstory.description, pawn);
		}
		return result;
	}

	public static List<IntVec3> GetNearbyCells(Pawn pawn, int distance = 5)
	{
		List<IntVec3> cells = new List<IntVec3>();
		IntVec3 facing = pawn.Rotation.FacingCell;
		for (int i = 1; i <= distance; i++)
		{
			IntVec3 targetCell = pawn.Position + facing * i;
			for (int offset = -1; offset <= 1; offset++)
			{
				IntVec3 cell = new IntVec3(targetCell.x + offset, targetCell.y, targetCell.z);
				if (cell.InBounds(pawn.Map))
				{
					cells.Add(cell);
				}
			}
		}
		return cells;
	}

	private static List<IntVec3> GetNearbyCellsRadial(Pawn pawn, int radius, bool sameRoomOnly)
	{
		Map map = pawn.Map;
		IntVec3 origin = pawn.Position;
		Room room = null;
		if (sameRoomOnly)
		{
			room = origin.GetRoom(map);
		}
		List<IntVec3> cells = new List<IntVec3>(128);
		foreach (IntVec3 c in GenRadial.RadialCellsAround(origin, radius, useCenter: true))
		{
			if (!c.InBounds(map))
			{
				continue;
			}
			if (sameRoomOnly && room != null)
			{
				Room r2 = c.GetRoom(map);
				if (r2 != room)
				{
					continue;
				}
			}
			cells.Add(c);
		}
		return cells;
	}

	public static bool IsHiddenForPlayer(Thing thing)
	{
		if (thing?.def == null)
		{
			return false;
		}
		if (Find.HiddenItemsManager == null)
		{
			return false;
		}
		return Find.HiddenItemsManager.Hidden(thing.def);
	}

	public static List<NearbyAgg> CollectNearbyContext(Pawn pawn, int distance = 5, int maxPerKind = 12, int maxCellsToScan = 18, int maxThingsTotal = 200, int maxItemThings = 120)
	{
		if (pawn?.Map == null || pawn.Position == IntVec3.Invalid)
		{
			return new List<NearbyAgg>();
		}
		Map map = pawn.Map;
		Room room = pawn.GetRoom();
		bool sameRoomOnly = room != null && !room.PsychologicallyOutdoors;
		List<IntVec3> cells = GetNearbyCellsRadial(pawn, distance, sameRoomOnly);
		if (cells.Count > maxCellsToScan)
		{
			cells = cells.Take(maxCellsToScan).ToList();
		}
		Dictionary<string, NearbyAgg> aggs = new Dictionary<string, NearbyAgg>();
		HashSet<int> seenBuildingIds = new HashSet<int>();
		int processedTotal = 0;
		int processedItems = 0;
		foreach (IntVec3 cell in cells)
		{
			List<Thing> thingsHere = cell.GetThingList(map);
			if (thingsHere == null || thingsHere.Count == 0)
			{
				continue;
			}
			for (int i = 0; i < thingsHere.Count; i++)
			{
				if (processedTotal >= maxThingsTotal)
				{
					goto end_IL_0301;
				}
				Thing thing = thingsHere[i];
				if (thing?.def == null || thing.DestroyedOrNull() || (Find.HiddenItemsManager != null && Find.HiddenItemsManager.Hidden(thing.def)))
				{
					continue;
				}
				if (thing.def.category == ThingCategory.Item)
				{
					if (thing.Position.GetSlotGroup(map) != null)
					{
						continue;
					}
					processedItems++;
					if (processedItems > maxItemThings)
					{
						goto end_IL_0301;
					}
					if (thing.stackCount >= 1000 && thing.def.stackLimit < 1000)
					{
						continue;
					}
				}
				processedTotal++;
				if (thing is Pawn otherPawn)
				{
					if (otherPawn != pawn && otherPawn.Spawned && !otherPawn.Dead && otherPawn.RaceProps.Animal)
					{
						AddAgg(aggs, otherPawn, NearbyKind.Animal);
					}
					continue;
				}
				switch (thing.def.category)
				{
				case ThingCategory.Building:
					if (seenBuildingIds.Add(thing.thingIDNumber) && !IsWall(thing))
					{
						AddAgg(aggs, thing, NearbyKind.Building);
					}
					break;
				case ThingCategory.Item:
					AddAgg(aggs, thing, NearbyKind.Item);
					break;
				case ThingCategory.Plant:
					AddAgg(aggs, thing, NearbyKind.Plant);
					break;
				default:
					if (thing.def.IsFilth)
					{
						AddAgg(aggs, thing, NearbyKind.Filth);
					}
					break;
				}
			}
			continue;
			end_IL_0301:
			break;
		}
		return (from a in aggs.Values
			group a by a.Kind).SelectMany((IGrouping<NearbyKind, NearbyAgg> g) => g.OrderByDescending((NearbyAgg x) => x.Count).Take(maxPerKind)).ToList();
	}

	private static void AddAgg(Dictionary<string, NearbyAgg> aggs, Thing thing, NearbyKind kind)
	{
		ThingDef def = thing.def;
		TaggedString label = def.LabelCap;
		string key = $"{kind}|{def.defName}";
		if (!aggs.TryGetValue(key, out var agg))
		{
			agg = new NearbyAgg
			{
				Kind = kind,
				Key = key,
				Label = label,
				Count = 0,
				StackSum = 0
			};
		}
		agg.Count++;
		if (kind == NearbyKind.Item)
		{
			agg.StackSum += thing.stackCount;
		}
		aggs[key] = agg;
	}

	public static string FormatNearbyContext(List<NearbyAgg> aggs)
	{
		if (aggs == null || aggs.Count == 0)
		{
			return null;
		}
		IEnumerable<string> sections = new List<string>
		{
			FmtGroup(NearbyKind.Building, "Buildings"),
			FmtGroup(NearbyKind.Item, "Items"),
			FmtGroup(NearbyKind.Plant, "Plants"),
			FmtGroup(NearbyKind.Animal, "Animals"),
			FmtGroup(NearbyKind.Filth, "Filth")
		}.Where((string s) => !string.IsNullOrWhiteSpace(s));
		return string.Join("\n", sections);
		string FmtGroup(NearbyKind kind, string title)
		{
			List<NearbyAgg> list = aggs.Where((NearbyAgg a) => a.Kind == kind).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			IEnumerable<string> parts = list.Select((NearbyAgg a) => (kind == NearbyKind.Item) ? ((a.Count > 1) ? $"{a.Label} ×{a.StackSum} ({a.Count} stacks)" : $"{a.Label} ×{a.StackSum}") : ((a.Count > 1) ? $"{a.Label} ×{a.Count}" : a.Label));
			return title + ": " + string.Join(", ", parts);
		}
	}

	public static string CollectNearbyContextText(Pawn pawn, int distance = 5, int maxPerKind = 12, int maxCellsToScan = 18, int maxThingsTotal = 200, int maxItemThings = 120)
	{
		List<NearbyAgg> aggs = CollectNearbyContext(pawn, distance, maxPerKind, maxCellsToScan, maxThingsTotal, maxItemThings);
		return FormatNearbyContext(aggs);
	}
}
