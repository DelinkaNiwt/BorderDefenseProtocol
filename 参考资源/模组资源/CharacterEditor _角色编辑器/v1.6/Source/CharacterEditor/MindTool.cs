using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class MindTool
{
	internal static string GetInspirationTooltip(this Pawn p)
	{
		return (p == null || p.Inspiration == null || p.Inspiration.def.beginLetter.NullOrEmpty()) ? "" : p.Inspiration.def.beginLetter.Formatted(p.NameShortColored, p.Named("PAWN")).AdjustedFor(p).ToString();
	}

	internal static string GetMentalStateTooltip(this Pawn p)
	{
		return (p == null || p.MentalState == null) ? "" : p.MentalState.GetBeginLetterText().ToString();
	}

	internal static Dictionary<EType, HashSet<ThoughtDef>> GetAllThoughtLists()
	{
		HashSet<ThoughtDef> hashSet = new HashSet<ThoughtDef>();
		HashSet<ThoughtDef> hashSet2 = new HashSet<ThoughtDef>();
		HashSet<ThoughtDef> hashSet3 = new HashSet<ThoughtDef>();
		HashSet<ThoughtDef> hashSet4 = new HashSet<ThoughtDef>();
		HashSet<ThoughtDef> hashSet5 = new HashSet<ThoughtDef>();
		HashSet<ThoughtDef> hashSet6 = new HashSet<ThoughtDef>();
		HashSet<ThoughtDef> hashSet7 = new HashSet<ThoughtDef>();
		foreach (ThoughtDef item in DefTool.ListAll<ThoughtDef>())
		{
			try
			{
				if (item.IsNullOrEmpty() || hashSet7.Contains(item) || item.GetThoughtLabel().NullOrEmpty())
				{
					continue;
				}
				hashSet7.Add(item);
				if (item.IsTypeOf<Thought_Memory>())
				{
					if (item.IsTrulySocial())
					{
						hashSet4.Add(item);
					}
					else
					{
						hashSet.Add(item);
					}
				}
				else if (item.IsTypeOf<Thought_Situational>())
				{
					if (item.IsTrulySocial())
					{
						hashSet5.Add(item);
					}
					else
					{
						hashSet2.Add(item);
					}
				}
				else
				{
					hashSet6.Add(item);
				}
			}
			catch
			{
			}
		}
		if (Prefs.DevMode)
		{
			MessageTool.Show("total=" + hashSet7.Count + ", mem=" + hashSet.Count + ", memsoz=" + hashSet4.Count + ", situ=" + hashSet2.Count + ", situsoz=" + hashSet5.Count + ", not=" + hashSet6.Count);
		}
		hashSet = hashSet.OrderBy((ThoughtDef t) => t.GetThoughtLabel()).ToHashSet();
		hashSet4 = hashSet4.OrderBy((ThoughtDef t) => t.GetThoughtLabel()).ToHashSet();
		hashSet2 = hashSet2.OrderBy((ThoughtDef t) => t.GetThoughtLabel()).ToHashSet();
		hashSet5 = hashSet5.OrderBy((ThoughtDef t) => t.GetThoughtLabel()).ToHashSet();
		hashSet6 = hashSet6.OrderBy((ThoughtDef t) => t.GetThoughtLabel()).ToHashSet();
		hashSet7 = hashSet7.OrderBy((ThoughtDef t) => t.GetThoughtLabel()).ToHashSet();
		Dictionary<EType, HashSet<ThoughtDef>> dictionary = new Dictionary<EType, HashSet<ThoughtDef>>();
		dictionary.Add(EType.ThoughtMemory, hashSet);
		dictionary.Add(EType.ThoughtMemorySocial, hashSet4);
		dictionary.Add(EType.ThoughtSituational, hashSet2);
		dictionary.Add(EType.ThoughtSituationalSocial, hashSet5);
		dictionary.Add(EType.ThoughtUnsupported, hashSet6);
		dictionary.Add(EType.ThoughtsAll, hashSet7);
		return dictionary;
	}

	internal static List<MentalStateDef> GetAllMentalStates()
	{
		List<MentalStateDef> list = (from td in DefDatabase<MentalStateDef>.AllDefs
			where td != null && !string.IsNullOrEmpty(td.label)
			orderby td.label
			select td).ToList();
		list.Insert(0, null);
		return list;
	}

	internal static List<InspirationDef> GetAllInspirations()
	{
		List<InspirationDef> list = (from td in DefDatabase<InspirationDef>.AllDefs
			where td != null && !string.IsNullOrEmpty(td.label)
			orderby td.label
			select td).ToList();
		list.Insert(0, null);
		return list;
	}

	internal static string GetAllNeedsAsSeparatedString(this Pawn p)
	{
		if (!p.HasNeedsTracker() && !p.needs.AllNeeds.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Need allNeed in p.needs.AllNeeds)
		{
			text = text + allNeed.def.defName + "|";
			text += allNeed.CurLevelPercentage;
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static Need GetNeedForThis(this Pawn p, NeedDef n)
	{
		return p.HasNeedsTracker() ? p.needs.TryGetNeed(n) : null;
	}

	internal static void SetNeeds(this Pawn p, string s)
	{
		if (s.NullOrEmpty() || !p.HasNeedsTracker())
		{
			return;
		}
		try
		{
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length != 2)
				{
					continue;
				}
				NeedDef needDef = DefTool.NeedDef(array3[0]);
				if (needDef != null)
				{
					Need needForThis = p.GetNeedForThis(needDef);
					if (needForThis != null)
					{
						needForThis.CurLevelPercentage = array3[1].AsFloat();
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
