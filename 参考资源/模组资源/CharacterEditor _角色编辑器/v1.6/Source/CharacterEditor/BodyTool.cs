using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class BodyTool
{
	internal static List<BodyTypeDef> GetBodyDefList(this Pawn pawn, bool restricted = false)
	{
		if (pawn.IsAlienRace())
		{
			List<BodyTypeDef> list = pawn.AlienPartGenerator_GetBodyTypes();
			if (list != null)
			{
				if (restricted && pawn.ageTracker != null)
				{
					List<BodyTypeDef> list2 = new List<BodyTypeDef>();
					bool flag = false;
					foreach (BodyTypeDef item in list)
					{
						flag = pawn.ageTracker.Adult && !item.defName.ToLower().Contains("baby") && !item.defName.ToLower().Contains("child");
						if (flag)
						{
							flag = ((pawn.gender == Gender.Female) ? (!item.defName.ToLower().Contains("male")) : (pawn.gender != Gender.Male || !item.defName.ToLower().Contains("female")));
						}
						if (flag)
						{
							list2.Add(item);
						}
					}
					return list2;
				}
				return list;
			}
		}
		return pawn.GetBodyList();
	}

	internal static List<BodyPartGroupDef> ListAllBodyPartGroupDefs(bool insertNull)
	{
		List<BodyPartGroupDef> list = (from td in DefDatabase<BodyPartGroupDef>.AllDefs
			where td.modContentPack.IsCoreMod
			orderby td.label
			select td).ToList();
		list.AddRange((from td in DefDatabase<BodyPartGroupDef>.AllDefs
			where !td.modContentPack.IsCoreMod
			orderby td.label
			select td).ToList());
		if (insertNull)
		{
			list.Insert(0, null);
		}
		return list;
	}

	internal static List<BodyTypeDef> GetBodyList(this Pawn pawn)
	{
		if (pawn == null || pawn.story == null)
		{
			return null;
		}
		bool flag = pawn.story.bodyType?.modContentPack?.Name == "Alien Vs Predator";
		List<BodyTypeDef> list = CEditor.API.ListOf<BodyTypeDef>(EType.Bodies);
		List<BodyTypeDef> list2 = new List<BodyTypeDef>();
		bool flag2 = false;
		foreach (BodyTypeDef item in list)
		{
			try
			{
				string text = pawn.Drawer.renderer.BodyGraphic.path;
				if (text.Contains("/Naked"))
				{
					text = text.SubstringTo("/Naked_", withoutIt: false) + item.defName + "_south";
				}
				else if (text.Contains("_Naked_"))
				{
					text = text.SubstringTo("_Naked_", withoutIt: false) + item.defName + "_south";
				}
				else if (text.Contains("Naked_"))
				{
					text = text.SubstringBackwardTo("Naked_") + "Naked_" + item.defName + "_south";
				}
				flag2 = TextureTool.TestTexturePath(text, showError: false);
				if (flag)
				{
					flag2 = pawn.kindDef.modContentPack == item.modContentPack && flag2;
				}
				if (flag2)
				{
					list2.Add(item);
				}
			}
			catch
			{
			}
		}
		return list2;
	}

	internal static string GetBodyTypeDefName(this Pawn p)
	{
		return (p != null && p.story != null && p.story.bodyType != null) ? p.story.bodyType.defName : "";
	}

	internal static string GetBodyTypeName(this Pawn pawn)
	{
		TaggedString? taggedString = pawn?.story?.bodyType?.defName.Translate();
		string text = (taggedString.HasValue ? ((string)taggedString.GetValueOrDefault()) : null);
		return text ?? "";
	}

	internal static string RemoveRotEndings(this string t)
	{
		return t.Replace("_south", "").Replace("_east", "").Replace("_north", "")
			.Replace("_west", "");
	}

	internal static void SetBodyParts(this Pawn p, string bodyDefName, string headTypeDef, string headAddons, string hairDefName, PawnKindDef pkd, int parCount)
	{
		try
		{
			p.SetBodyByDefName(bodyDefName);
			if (parCount > 47 && !headTypeDef.NullOrEmpty())
			{
				p.SetHeadTypeDef(headTypeDef);
			}
			p.SetHeadAddonsFromSeparatedString(headAddons);
			HairDef hairDef = DefTool.HairDef(hairDefName);
			if (hairDef != null)
			{
				p.SetHair(hairDef);
			}
			else if (!pkd.IsAnimal() && !pkd.RaceProps.Humanlike)
			{
				MessageTool.Show("Hair not found:" + hairDefName);
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetBodyByDefName(this Pawn p, string defName)
	{
		if (!p.HasStoryTracker() || defName.NullOrEmpty())
		{
			return;
		}
		try
		{
			BodyTypeDef bodyTypeDef = DefTool.BodyTypeDef(defName);
			if (bodyTypeDef == null)
			{
				if (!p.kindDef.IsAnimal() && !p.kindDef.RaceProps.Humanlike)
				{
					MessageTool.Show("BodyTypeDef not found: " + defName);
				}
			}
			else
			{
				p.SetBody(bodyTypeDef);
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetBody(this Pawn p, BodyTypeDef b)
	{
		if (p.HasStoryTracker() && b != null)
		{
			p.story.bodyType = b;
			p.SetDirty();
		}
	}

	internal static void SetBody(this Pawn p, bool next, bool random)
	{
		if (p.HasStoryTracker())
		{
			List<BodyTypeDef> bodyDefList = p.GetBodyDefList();
			int index = bodyDefList.IndexOf(p.story.bodyType);
			index = bodyDefList.NextOrPrevIndex(index, next, random);
			if (!bodyDefList.NullOrEmpty())
			{
				p.SetBody(bodyDefList[index]);
			}
		}
	}
}
