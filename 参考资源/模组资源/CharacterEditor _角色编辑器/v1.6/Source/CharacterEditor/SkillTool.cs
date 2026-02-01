using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

public static class SkillTool
{
	internal const string CO_SKILLRECORDSTATE = "cachedTotallyDisabled";

	internal const string CO_CACHEDDISABLEDWORKTYPES = "cachedDisabledWorkTypes";

	internal const string CO_CACHEDDISABLEDWORKTYPESPERMANENT = "cachedDisabledWorkTypesPermanent";

	public static string GetAllSkillsAsSeparatedString(this Pawn p)
	{
		if (!p.HasSkillTracker() || p.skills.skills.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (SkillRecord skill in p.skills.skills)
		{
			text += skill.GetSkillAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	public static string GetSkillAsSeparatedString(this SkillRecord s)
	{
		if (s == null || s.def.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = text + s.def.defName + "|";
		text = text + s.Level + "|";
		string text2 = text;
		int passion = (int)s.passion;
		text = text2 + passion + "|";
		text = text + s.xpSinceLastLevel + "|";
		return text + s.xpSinceMidnight;
	}

	public static void SetSkillsFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasSkillTracker())
		{
			return;
		}
		try
		{
			foreach (SkillRecord skill in p.skills.skills)
			{
				skill.Level = 0;
				skill.levelInt = 0;
				skill.passion = Passion.None;
				skill.xpSinceLastLevel = 0f;
				skill.xpSinceMidnight = 0f;
			}
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.Split(new string[1] { ":" }, StringSplitOptions.None);
			string[] array2 = array;
			foreach (string text in array2)
			{
				string[] array3 = text.Split(new string[1] { "|" }, StringSplitOptions.None);
				if (array3.Length == 5)
				{
					SkillDef skillDef = DefTool.SkillDef(array3[0]);
					if (skillDef != null)
					{
						p.SetSkill(skillDef, array3[1].AsInt32(), (Passion)array3[2].AsInt32(), array3[3].AsFloat(), array3[4].AsFloat());
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	public static void AddSkill(this Pawn p, SkillDef skd, int level, Passion passion, float xpSince, float xpMidnight)
	{
		if (p != null && skd != null)
		{
			SkillRecord skillRecord = new SkillRecord(p, skd);
			skillRecord.passion = passion;
			skillRecord.Level = level;
			skillRecord.xpSinceLastLevel = xpSince;
			skillRecord.xpSinceMidnight = xpMidnight;
			p.skills.skills.Add(skillRecord);
		}
	}

	public static void CopySkillFromSkillRecord(this Pawn p, SkillRecord sr)
	{
		p.SetSkill(sr.def, sr.Level, sr.passion, sr.xpSinceLastLevel, sr.xpSinceMidnight);
	}

	public static void DisableSkillsFromList(this Pawn p, List<SkillDef> l)
	{
		if (p == null || l.NullOrEmpty())
		{
			return;
		}
		foreach (SkillDef item in l)
		{
			p.SkillDisable(item, BoolUnknown.True);
		}
	}

	public static void EnableAllSkills(this Pawn p)
	{
		if (p == null || p.skills == null || p.skills.skills.NullOrEmpty())
		{
			return;
		}
		p.SetMemberValue("cachedDisabledWorkTypes", new List<WorkTypeDef>());
		p.SetMemberValue("cachedDisabledWorkTypesPermanent", new List<WorkTypeDef>());
		foreach (SkillRecord skill in p.skills.skills)
		{
			skill.SetMemberValue("cachedTotallyDisabled", BoolUnknown.False);
		}
		p.skills?.Notify_SkillDisablesChanged();
		p.Recalculate_WorkTypes();
	}

	public static string GetIncapableOf(this Pawn pawn, out string toolTip)
	{
		toolTip = "";
		if (pawn == null || pawn.story == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (WorkTags allSelectedItem in pawn.story.DisabledWorkTagsBackstoryAndTraits.GetAllSelectedItems<WorkTags>())
		{
			if (allSelectedItem != WorkTags.None)
			{
				list2.Add(allSelectedItem.LabelTranslated());
			}
			foreach (WorkTypeDef allDef in DefDatabase<WorkTypeDef>.AllDefs)
			{
				if ((allDef.workTags & allSelectedItem) != WorkTags.None && !list.Contains(allDef.labelShort))
				{
					list.Add(allDef.labelShort);
				}
			}
		}
		toolTip = toolTip + Label.DISABLEDWORKTAGS + ":\n";
		foreach (string item in list)
		{
			toolTip = toolTip + "- " + item + "\n";
		}
		if (pawn.skills != null && pawn.skills.skills != null)
		{
			toolTip = toolTip + "\n" + Label.DISABLEDSKILLS + ":\n";
			foreach (SkillRecord skill in pawn.skills.skills)
			{
				if (pawn.story.Childhood != null && skill.def.IsDisabled(pawn.story.Childhood.workDisables, pawn.story.Childhood.DisabledWorkTypes))
				{
					toolTip += "- " + skill.def.LabelCap + " (" + pawn.story.Childhood.TitleCapFor(pawn.gender) + ")" + "\n";
				}
				if (pawn.story.Adulthood != null && skill.def.IsDisabled(pawn.story.Adulthood.workDisables, pawn.story.Adulthood.DisabledWorkTypes))
				{
					toolTip = toolTip + "- " + skill.def.skillLabel + " (" + pawn.story.Adulthood.TitleCapFor(pawn.gender) + ")\n";
				}
			}
		}
		string text = "";
		foreach (string item2 in list2)
		{
			text = text + item2 + ", ";
		}
		if (text.EndsWith(", "))
		{
			text = text.Substring(0, text.Length - 2);
		}
		return text;
	}

	public static int GetSkillIndex(this SkillDef skill)
	{
		if (skill == SkillDefOf.Shooting)
		{
			return 0;
		}
		if (skill == SkillDefOf.Melee)
		{
			return 1;
		}
		if (skill == SkillDefOf.Construction)
		{
			return 2;
		}
		if (skill == SkillDefOf.Mining)
		{
			return 3;
		}
		if (skill == SkillDefOf.Cooking)
		{
			return 4;
		}
		if (skill == SkillDefOf.Plants)
		{
			return 5;
		}
		if (skill == SkillDefOf.Animals)
		{
			return 6;
		}
		if (skill == SkillDefOf.Crafting)
		{
			return 7;
		}
		if (skill == SkillDefOf.Artistic)
		{
			return 8;
		}
		if (skill == SkillDefOf.Medicine)
		{
			return 9;
		}
		if (skill == SkillDefOf.Social)
		{
			return 10;
		}
		if (skill == SkillDefOf.Intellectual)
		{
			return 11;
		}
		return 12;
	}

	public static bool ListHasOneOrMoreMatches(List<WorkTags> l1, List<WorkTags> l2)
	{
		foreach (WorkTags item in l1)
		{
			foreach (WorkTags item2 in l2)
			{
				if (item == item2)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static List<WorkTags> ListOfDiabledWorkTagsByBackstory(this Pawn p)
	{
		if (p == null || p.story == null)
		{
			return new List<WorkTags>();
		}
		return p.story.DisabledWorkTagsBackstoryAndTraits.GetAllSelectedItems<WorkTags>().ToList();
	}

	public static List<WorkTags> ListOfDisablingWorkTagsForSkill(this SkillDef s)
	{
		if (s == null)
		{
			return new List<WorkTags>();
		}
		return s.disablingWorkTags.GetAllSelectedItems<WorkTags>().ToList();
	}

	public static void PasteSkills(this Pawn p, List<SkillRecord> l)
	{
		if (p == null || p.skills == null || p.skills.skills.NullOrEmpty() || l.NullOrEmpty())
		{
			return;
		}
		foreach (SkillRecord item in l)
		{
			p.CopySkillFromSkillRecord(item);
		}
		p.skills?.Notify_SkillDisablesChanged();
		p.Recalculate_WorkTypes();
	}

	public static void SetSkill(this Pawn p, SkillDef skd, int level, Passion passion, float xpSince, float xpMidnight)
	{
		if (p == null || skd == null)
		{
			return;
		}
		foreach (SkillRecord skill in p.skills.skills)
		{
			if (skill.def.defName == skd.defName)
			{
				skill.passion = passion;
				skill.levelInt = level - skill.Aptitude;
				skill.xpSinceLastLevel = xpSince;
				skill.xpSinceMidnight = xpMidnight;
				break;
			}
		}
	}

	public static void SkillDisable(this Pawn p, SkillRecord skillRecord, BoolUnknown val)
	{
		if (p != null && p.skills != null)
		{
			skillRecord.SetMemberValue("cachedTotallyDisabled", val);
		}
	}

	public static void SkillDisable(this Pawn p, SkillDef skill, BoolUnknown val)
	{
		if (p != null && p.skills != null)
		{
			SkillRecord skill2 = p.skills.GetSkill(skill);
			skill2.SetMemberValue("cachedTotallyDisabled", val);
		}
	}
}
