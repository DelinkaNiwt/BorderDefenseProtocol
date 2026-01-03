using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AlienRace;

public class ThoughtSettings
{
	public List<ThoughtDef> cannotReceiveThoughts;

	public bool cannotReceiveThoughtsAtAll;

	public List<ThoughtDef> canStillReceiveThoughts;

	public static Dictionary<ThoughtDef, List<ThingDef_AlienRace>> thoughtRestrictionDict = new Dictionary<ThoughtDef, List<ThingDef_AlienRace>>();

	public List<ThoughtDef> restrictedThoughts = new List<ThoughtDef>();

	public ButcherThought butcherThoughtGeneral = new ButcherThought();

	public List<ButcherThought> butcherThoughtSpecific = new List<ButcherThought>();

	public AteThought ateThoughtGeneral = new AteThought();

	public List<AteThought> ateThoughtSpecific = new List<AteThought>();

	private static readonly Dictionary<uint, bool> canGetThoughtCache = new Dictionary<uint, bool>();

	public List<ThoughtReplacer> replacerList;

	public ThoughtDef ReplaceIfApplicable(ThoughtDef def)
	{
		if (replacerList == null || replacerList.Select((ThoughtReplacer tr) => tr.replacer).Contains(def))
		{
			return def;
		}
		for (int i = 0; i < replacerList.Count; i++)
		{
			if (replacerList[i].original == def)
			{
				return replacerList[i].replacer ?? def;
			}
		}
		return def;
	}

	public ThoughtDef GetAteThought(ThingDef race, bool cannibal, bool ingredient)
	{
		return (ateThoughtSpecific?.FirstOrDefault((AteThought at) => at.raceList?.Contains(race) ?? false) ?? ateThoughtGeneral)?.GetThought(cannibal, ingredient);
	}

	public bool CanGetThought(ThoughtDef def)
	{
		def = ReplaceIfApplicable(def);
		if (cannotReceiveThoughtsAtAll)
		{
			List<ThoughtDef> list = canStillReceiveThoughts;
			if (list == null || !list.Contains(def))
			{
				return false;
			}
		}
		return !(cannotReceiveThoughts?.Contains(def) ?? false);
	}

	public static bool CanGetThought(ThoughtDef def, ThingDef race)
	{
		uint key = (uint)(def.shortHash | (race.shortHash << 16));
		if (!canGetThoughtCache.TryGetValue(key, out var canGetThought))
		{
			List<ThingDef_AlienRace> races;
			bool result = !thoughtRestrictionDict.TryGetValue(def, out races);
			canGetThoughtCache.Add(key, canGetThought = ((!(race is ThingDef_AlienRace alienProps)) ? result : ((races == null || races.Contains(alienProps)) && alienProps.alienRace.thoughtSettings.CanGetThought(def))));
		}
		return canGetThought;
	}

	public static bool CanGetThought(ThoughtDef def, Pawn pawn)
	{
		return CanGetThought(def, pawn.def);
	}
}
