using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class BackstoryTool
{
	internal static List<BackstoryDef> ListOfBackstories(bool isChildhood, bool notDisabling)
	{
		return (from td in DefDatabase<BackstoryDef>.AllDefs
			where td != null && (uint)td.slot == ((!isChildhood) ? 1u : 0u) && !string.IsNullOrEmpty(td.title) && (!notDisabling || td.DisabledWorkTypes.Count() == 0)
			orderby td.title
			select td).ToList();
	}

	internal static BackstoryDef GetBackstory(string s)
	{
		if (s.NullOrEmpty())
		{
			return null;
		}
		List<BackstoryDef> list = (from x in DefDatabase<BackstoryDef>.AllDefs
			where !x.IsNullOrEmpty() && !x.defName.NullOrEmpty()
			orderby x.defName
			select x).ToList();
		foreach (BackstoryDef item in list)
		{
			if (item.defName == s)
			{
				return item;
			}
		}
		BackstoryDef backstoryDef = list.RandomElement();
		MessageTool.Show("could not find backstory " + s + " loaded " + backstoryDef.defName + " instead");
		return backstoryDef;
	}

	internal static string GetBackstrory(this Pawn pawn, bool isChildhood)
	{
		return (!pawn.HasStoryTracker()) ? "" : ((!isChildhood) ? ((pawn.story.Adulthood == null) ? "" : pawn.story.Adulthood.defName) : ((pawn.story.Childhood == null) ? "" : pawn.story.Childhood.defName));
	}

	internal static void SetBackstory(this Pawn pawn, bool next, bool random, bool isChildhood, bool notDisabled)
	{
		if (pawn != null && pawn.story != null)
		{
			List<BackstoryDef> list = (from td in DefDatabase<BackstoryDef>.AllDefs
				where td != null && (uint)td.slot == ((!isChildhood) ? 1u : 0u) && !string.IsNullOrEmpty(td.title) && (!notDisabled || td.DisabledWorkTypes.Count() == 0)
				orderby td.title
				select td).ToList();
			int index = list.IndexOf(isChildhood ? pawn.story.Childhood : pawn.story.Adulthood);
			index = list.NextOrPrevIndex(index, next, random);
			BackstoryDef childhood = (isChildhood ? list[index] : pawn.story?.Childhood);
			BackstoryDef adulthood = ((!isChildhood) ? list[index] : pawn.story?.Adulthood);
			pawn.SetBackstory(childhood, adulthood);
		}
	}

	internal static void SetBackstory(this Pawn p, BackstoryDef childhood, BackstoryDef adulthood)
	{
		if (p == null || p.story == null)
		{
			return;
		}
		try
		{
			p.story.Childhood = childhood;
			p.story.Adulthood = adulthood;
			BackstoryDef childhood2 = p.story.Childhood;
			BackstoryDef adulthood2 = p.story.Adulthood;
			if (p.skills != null && p.skills.skills != null)
			{
				p.EnableAllSkills();
				foreach (SkillRecord skill in p.skills.skills)
				{
					if (p.story.Childhood != null && skill.def.IsDisabled(p.story.Childhood.workDisables, p.story.Childhood.DisabledWorkTypes))
					{
						p.SkillDisable(skill.def, BoolUnknown.True);
					}
					if (p.story.Adulthood != null && skill.def.IsDisabled(p.story.Adulthood.workDisables, p.story.Adulthood.DisabledWorkTypes))
					{
						p.SkillDisable(skill.def, BoolUnknown.True);
					}
				}
			}
			p.skills?.Notify_SkillDisablesChanged();
			p.Recalculate_WorkTypes();
			MeditationFocusTypeAvailabilityCache.ClearFor(p);
			StatsReportUtility.Reset();
			CEditor.API.UpdateGraphics();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
