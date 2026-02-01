using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class FactionTool
{
	internal static Dictionary<string, Faction> DicFactions => CEditor.API.Get<Dictionary<string, Faction>>(EType.Factions);

	internal static List<Faction> FactionEnemies(this Pawn pawn)
	{
		if (pawn == null || Find.FactionManager == null)
		{
			return new List<Faction>();
		}
		List<Faction> result = new List<Faction>();
		try
		{
			result = Find.FactionManager.AllFactionsInViewOrder.Where((Faction f) => f != pawn.Faction && pawn.Faction.RelationWith(f) != null && !pawn.IsFriendlyFaction(f)).ToList();
		}
		catch
		{
		}
		return result;
	}

	internal static Dictionary<string, Faction> GetDicOfFactions(bool addPseudoFaction = true, bool addCreatures = true, bool addHumanoids = true)
	{
		Dictionary<string, Faction> dictionary = new Dictionary<string, Faction>();
		List<Faction> list = Current.Game.World.factionManager.AllFactions.ToList();
		list.SortBy((Faction s) => s.Name);
		if (addPseudoFaction)
		{
			dictionary.Add(Label.HUMANOID, null);
			dictionary.Add(Label.COLONISTS, Faction.OfPlayer);
			dictionary.Add(Label.COLONYANIMALS, Faction.OfPlayer);
			if (!CEditor.InStartingScreen)
			{
				dictionary.Add(Label.WILDANIMALS, null);
			}
		}
		foreach (Faction item in list)
		{
			if ((addCreatures || !item.IsCreature()) && (addHumanoids || !item.IsHumanlike()))
			{
				dictionary.AddLabeled((item.GetCallLabel() ?? item.Name).CapitalizeFirst(), item);
			}
		}
		return dictionary;
	}

	internal static string GetFactionFullDesc(this Pawn pawn, Faction f = null)
	{
		if (pawn == null || pawn.Faction.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = ((f == null) ? pawn.Faction.Name.CapitalizeFirst() : f.Name.CapitalizeFirst());
		text += "\n";
		if (f == null && pawn.Faction != Faction.OfPlayer)
		{
			f = Faction.OfPlayer;
		}
		if (f != null)
		{
			FactionRelation factionRelation = pawn.Faction.RelationWith(f, allowNull: true);
			string text2 = "";
			if (factionRelation != null)
			{
				int num = 0;
				try
				{
					num = pawn.Faction.GoodwillWith(f);
				}
				catch
				{
				}
				text2 = num + " ";
				FactionRelationKind kind = factionRelation.kind;
				text += (text2 + kind.GetLabel() + "\n").Colorize(kind.GetColor());
			}
		}
		text += "\n";
		return (f != null) ? (text + f.def?.description) : (text + pawn.Faction.def?.description);
	}

	internal static Color GetFacionColor(this Faction f)
	{
		return f.HasFactionColor() ? ColorsFromSpectrum.Get(f.def.colorSpectrum, f.colorFromSpectrum) : Color.white;
	}

	internal static Color GetFacionColor(this Pawn pawn)
	{
		return pawn?.Faction.GetFacionColor() ?? Color.white;
	}

	internal static bool HasFactionColor(this Faction f)
	{
		return !f.IsNullOrEmpty() && !f.def.colorSpectrum.NullOrEmpty();
	}

	internal static void ChangeFaction(this Pawn p, Faction f)
	{
		if (p != null && f != null && p.Faction != f)
		{
			p.SetFactionDirect(f);
		}
	}

	internal static bool CanCreateRelations(this Faction f, PawnKindDef pkd)
	{
		return f.IsNotInsectXenoMechZombie() && pkd.modContentPack != null && (pkd.modContentPack.IsCoreMod || pkd.modContentPack.Name == "Royalty");
	}

	internal static bool IsAbomination(this Faction f)
	{
		return !f.IsNullOrEmpty() && f.def.defName == "Abomination";
	}

	internal static bool IsAnimal(this Faction f, string key)
	{
		return f.IsNotInsectXenoMechZombie() && key != Label.COLONISTS && (key == Label.COLONYANIMALS || key == Label.WILDANIMALS);
	}

	internal static bool IsCreature(this Faction f)
	{
		return f.IsInsektoid() || f.IsXeno() || f.IsMechanoid() || f.IsAbomination() || f.IsZombie();
	}

	internal static bool IsFriendlyFaction(this Pawn pawn, Faction f)
	{
		return pawn != null && (f == pawn.Faction || !f.HostileTo(pawn.Faction));
	}

	internal static bool IsHumanlike(this Faction f)
	{
		return !f.IsNullOrEmpty() && f.def.humanlikeFaction;
	}

	internal static bool IsHumanoid(this Faction f, string key)
	{
		return f.IsNotInsectXenoMechZombie() && key != Label.COLONYANIMALS && key != Label.WILDANIMALS;
	}

	internal static bool IsInsektoid(this Faction f)
	{
		return f == Faction.OfInsects;
	}

	internal static bool IsMechanoid(this Faction f)
	{
		return f == Faction.OfMechanoids;
	}

	internal static bool IsNotInsectXenoMechZombie(this Faction f)
	{
		return !f.IsInsektoid() && !f.IsXeno() && !f.IsMechanoid() && !f.IsAbomination() && !f.IsZombie();
	}

	internal static bool IsNullOrEmpty(this Faction f)
	{
		return f == null || f.def == null;
	}

	internal static bool IsOther(this Faction f, string key)
	{
		return !f.IsNullOrEmpty() && key != Label.COLONISTS && key != Label.COLONYANIMALS;
	}

	internal static bool IsXeno(this Faction f)
	{
		return !f.IsNullOrEmpty() && f.Name.ToLower().Contains("xenomorph");
	}

	internal static bool IsZombie(this Faction f)
	{
		return !f.IsNullOrEmpty() && f.def.defName == "Zombies";
	}

	internal static Faction ThisOrDefault(this Faction f)
	{
		return f ?? GetDicOfFactions(addPseudoFaction: false, addCreatures: false).Values.RandomElement();
	}

	internal static string GetPawnFactionAsSeparatedString(this Pawn p)
	{
		return (p != null && p.Faction != null) ? (p.Faction.Name + "|" + p.Faction.def.defName) : (Faction.OfPlayer.Name + "|" + Faction.OfPlayer.def.defName);
	}

	internal static Faction GetBySeparatedString(string s, Faction factionFallback)
	{
		if (s.NullOrEmpty())
		{
			return factionFallback;
		}
		Faction faction = null;
		try
		{
			string[] array = s.SplitNo("|");
			if (array.Length == 2)
			{
				string key = array[0];
				string text = array[1];
				faction = DicFactions.GetValue(key);
				if (faction == null)
				{
					foreach (Faction value in DicFactions.Values)
					{
						if (value != null && value.def.defName == text)
						{
							faction = value;
							break;
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
		return faction ?? factionFallback;
	}
}
