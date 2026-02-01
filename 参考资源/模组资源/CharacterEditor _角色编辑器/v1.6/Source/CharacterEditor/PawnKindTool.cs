using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class PawnKindTool
{
	internal static string GetPawnKindDefname(this Pawn p)
	{
		return (p != null && p.kindDef != null) ? p.kindDef.defName : "";
	}

	internal static bool IsFromMod(this PawnKindDef pkd, string modname)
	{
		return modname.NullOrEmpty() || (pkd.modContentPack != null && pkd.modContentPack.Name == modname);
	}

	internal static bool IsTribalOrBoss(this PawnKindDef pkd)
	{
		return pkd != null && pkd.defName != null && (pkd.defName.Contains("Tribal_") || pkd.defName.Contains("Empire_Fighter_Champion") || pkd.defName.Contains("ZhthyhlMid"));
	}

	internal static bool IsDroidColonist(this PawnKindDef pkd)
	{
		return pkd != null && pkd.defName != null && pkd.defName.EndsWith("DroidColonist");
	}

	internal static bool IsDorfbewohner(this PawnKindDef pkd)
	{
		return pkd != null && pkd.defName != null && pkd.defName.StartsWith("Villager");
	}

	internal static bool IsXeno(this PawnKindDef pkd)
	{
		return pkd.race != null && pkd.modContentPack != null && pkd.modContentPack.Name == "Alien Vs Predator" && !pkd.RaceProps.Humanlike && pkd.race.defName != null && !pkd.race.defName.ToLower().Contains("yautja");
	}

	internal static bool IsAbomination(this PawnKindDef pkd)
	{
		return pkd != null && pkd.defName != null && pkd.defName.StartsWith("Abomination");
	}

	internal static bool IsInsektoid(this PawnKindDef pkd)
	{
		return pkd.RaceProps.FleshType == FleshTypeDefOf.Insectoid;
	}

	internal static bool IsMechanoid(this PawnKindDef pkd)
	{
		return pkd.RaceProps.FleshType == FleshTypeDefOf.Mechanoid;
	}

	internal static bool IsZombie(this PawnKindDef pkd)
	{
		return pkd.defName == "Zombie";
	}

	internal static bool IsAnimal(this PawnKindDef pkd)
	{
		return pkd != null && pkd.RaceProps != null && pkd.RaceProps.Animal;
	}

	internal static PawnKindDef ThisOrFromList(this PawnKindDef pkd, List<PawnKindDef> lpkd)
	{
		if (pkd == null)
		{
			List<PawnKindDef> list = new List<PawnKindDef>();
			list.AddRange(lpkd);
			list.Remove(null);
			return list.RandomElementWithFallback();
		}
		return pkd;
	}

	internal static PawnKindDef ThisOrRandom(this PawnKindDef pkd, string modname)
	{
		return pkd ?? GetRandomPawnKindDef(modname);
	}

	internal static PawnKindDef GetRandomPawnKindDef(string modname)
	{
		HashSet<PawnKindDef> source = DefTool.ListByMod<PawnKindDef>(modname);
		return source.RandomElement();
	}

	internal static List<PawnKindDef> ThisOrDefault(this List<PawnKindDef> l)
	{
		return (l.NullOrEmpty() || (l.Count == 1 && l.First() == null)) ? (from pkd in CEditor.API.ListOf<PawnKindDef>(EType.PawnKindListed)
			where pkd != null
			select pkd).ToList() : l;
	}

	internal static List<PawnKindDef> GetHumanlikes()
	{
		return (from td in DefDatabase<PawnKindDef>.AllDefs
			where td.race != null && td.race.label != null && td.RaceProps.Humanlike
			orderby td.race.label
			select td).ToList();
	}

	internal static List<PawnKindDef> GetAnimals()
	{
		return (from td in DefDatabase<PawnKindDef>.AllDefs
			where td.race != null && td.race.label != null && td.RaceProps.Animal
			orderby td.race.label
			select td).ToList();
	}

	internal static List<PawnKindDef> GetOther()
	{
		return (from td in DefDatabase<PawnKindDef>.AllDefs
			where td.race != null && td.race.label != null && !td.RaceProps.Animal && !td.RaceProps.Humanlike
			orderby td.race.label
			select td).ToList();
	}

	internal static List<PawnKindDef> GetPawnKindListxx(Faction f, string key = null)
	{
		key = key ?? CEditor.ListName;
		PawnxTool.SetPawnKindFlags(key, f, out var _, out var humanoid, out var animal, out var other, out var xeno, out var _);
		List<PawnKindDef> list = (humanoid ? CEditor.API.ListOf<PawnKindDef>(EType.PawnKindHuman) : null);
		List<PawnKindDef> list2 = ((animal || f == Faction.OfInsects) ? CEditor.API.ListOf<PawnKindDef>(EType.PawnKindAnimal) : null);
		List<PawnKindDef> list3 = (other ? CEditor.API.ListOf<PawnKindDef>(EType.PawnKindOther) : null);
		List<PawnKindDef> list4 = new List<PawnKindDef>();
		if (!list.NullOrEmpty())
		{
			list4.AddRange(list);
		}
		if (!list2.NullOrEmpty())
		{
			list4.AddRange(list2);
		}
		if (!list3.NullOrEmpty())
		{
			list4.AddRange(list3);
		}
		list4 = (f.IsInsektoid() ? list4.Where((PawnKindDef td) => td.RaceProps.FleshType == FleshTypeDefOf.Insectoid).ToList() : ((!f.IsMechanoid()) ? (from td in list4
			where td.defName != "Zombie"
			orderby td.label
			select td).ToList() : list4.Where((PawnKindDef td) => td.RaceProps.FleshType == FleshTypeDefOf.Mechanoid).ToList()));
		if (xeno)
		{
			list4 = list4.Where((PawnKindDef td) => td.race != null && td.IsFromMod("Alien Vs Predator") && !td.RaceProps.Humanlike && td.race.defName != null && !td.race.defName.ToLower().Contains("yautja")).ToList();
		}
		return list4;
	}

	internal static HashSet<ThingDef> ListOfRaces(bool humanlike, bool nonhumanlike)
	{
		HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
		HashSet<ThingDef> hashSet2 = new HashSet<ThingDef>();
		IEnumerable<PawnKindDef> allDefs = DefDatabase<PawnKindDef>.AllDefs;
		foreach (PawnKindDef item in allDefs)
		{
			if (item == null || item.race == null || item.race.label == null)
			{
				continue;
			}
			if (item.RaceProps.Humanlike)
			{
				if (!hashSet.Contains(item.race))
				{
					hashSet.Add(item.race);
				}
			}
			else if (!hashSet2.Contains(item.race))
			{
				hashSet2.Add(item.race);
			}
		}
		bool flag = (humanlike && nonhumanlike) || (!humanlike && !nonhumanlike);
		HashSet<ThingDef> hashSet3 = new HashSet<ThingDef>();
		if (flag || humanlike)
		{
			hashSet3.AddRange(hashSet);
		}
		if (flag || nonhumanlike)
		{
			hashSet3.AddRange(hashSet2);
		}
		return hashSet3;
	}

	internal static HashSet<PawnKindDef> ListOfPawnKindDefByRace(ThingDef raceDef, bool humanlike, bool nonhumanlike)
	{
		IEnumerable<PawnKindDef> allDefs = DefDatabase<PawnKindDef>.AllDefs;
		bool all = (humanlike && nonhumanlike) || (!humanlike && !nonhumanlike);
		if (raceDef == null)
		{
			return (from td in allDefs
				where td != null && td.race != null && (all || td.RaceProps.Humanlike == humanlike)
				orderby td.label
				select td).ToHashSet();
		}
		return (from td in allDefs
			where td != null && td.race != null && td.race == raceDef && (all || td.RaceProps.Humanlike == humanlike)
			orderby td.label
			select td).ToHashSet();
	}

	internal static List<PawnKindDef> ListOfPawnKindDefByRaceName(Faction f, string key, string modname, string raceDefName)
	{
		List<PawnKindDef> list = ListOfPawnKindDef(f, key, modname);
		if (!raceDefName.NullOrEmpty())
		{
			list = list.Where((PawnKindDef td) => td.race.defName == raceDefName).ToList();
		}
		return list;
	}

	internal static List<PawnKindDef> ListOfPawnKindDef(Faction f, string key, string modname)
	{
		key = key ?? CEditor.ListName;
		PawnxTool.SetPawnKindFlags(key, f, out var _, out var humanoid, out var animal, out var other, out var xeno, out var _);
		List<PawnKindDef> list = (humanoid ? CEditor.API.ListOf<PawnKindDef>(EType.PawnKindHuman) : null);
		List<PawnKindDef> list2 = ((animal || f == Faction.OfInsects) ? CEditor.API.ListOf<PawnKindDef>(EType.PawnKindAnimal) : null);
		List<PawnKindDef> list3 = (other ? CEditor.API.ListOf<PawnKindDef>(EType.PawnKindOther) : null);
		List<PawnKindDef> list4 = new List<PawnKindDef>();
		if (!list.NullOrEmpty())
		{
			list4.AddRange(list);
		}
		if (!list2.NullOrEmpty())
		{
			list4.AddRange(list2);
		}
		if (!list3.NullOrEmpty())
		{
			list4.AddRange(list3);
		}
		if (f.IsInsektoid())
		{
			return list4.Where((PawnKindDef td) => td.IsFromMod(modname) && td.IsInsektoid()).ToList();
		}
		if (f.IsMechanoid())
		{
			return list4.Where((PawnKindDef td) => td.IsFromMod(modname) && td.IsMechanoid()).ToList();
		}
		if (xeno)
		{
			return list4.Where((PawnKindDef td) => td.IsFromMod(modname) && td.IsXeno()).ToList();
		}
		return (from td in list4
			where td.IsFromMod(modname) && !td.IsZombie()
			orderby td.label
			select td).ToList();
	}
}
