using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class HeadTool
{
	internal static string GetAllHeadAddonsAsSeparatedString(this Pawn p)
	{
		if (!p.IsAlienRace())
		{
			return "";
		}
		List<int> list = p.AlienRaceComp_GetAddonVariants();
		string text = "";
		if (!list.NullOrEmpty())
		{
			foreach (int item in list)
			{
				text += item;
				text += ":";
			}
			text = text.SubstringRemoveLast();
		}
		return text;
	}

	internal static void SetHeadAddonsFromSeparatedString(this Pawn p, string s)
	{
		if (s.NullOrEmpty() || !p.IsAlienRace())
		{
			return;
		}
		try
		{
			string[] array = s.SplitNo(":");
			List<int> list = new List<int>();
			if (array.Length != 0)
			{
				string[] array2 = array;
				foreach (string input in array2)
				{
					list.Add(input.AsInt32());
				}
			}
			p.AlienRaceComp_SetAddonVariants(list);
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void GetHeadPathFolderFor(this Pawn pawn, out string pathGenderized, out string pathNonGenderized)
	{
		pathGenderized = "";
		pathNonGenderized = "";
		if (!pawn.HasStoryTracker() || pawn.story.headType == null)
		{
			return;
		}
		string graphicPath = pawn.story.headType.graphicPath;
		graphicPath = graphicPath ?? "";
		if (Prefs.DevMode)
		{
			Log.Message("head graphic path=" + graphicPath);
		}
		graphicPath = graphicPath.SubstringBackwardTo("/");
		if (Prefs.DevMode)
		{
			Log.Message("heads path=" + graphicPath);
		}
		Gender gender = pawn.gender;
		if (graphicPath.StartsWith("Things/Pawn/Humanlike/Heads"))
		{
			pathNonGenderized = "Things/Pawn/Humanlike/Heads/";
			switch (gender)
			{
			case Gender.Female:
				pathGenderized = graphicPath.Replace("Male", "Female");
				break;
			case Gender.Male:
				pathGenderized = graphicPath.Replace("Female", "Male");
				break;
			}
		}
		else if (graphicPath.Contains("/Male") || graphicPath.Contains("/Female"))
		{
			pathNonGenderized = (graphicPath.Contains("/Male") ? graphicPath.SubstringBackwardTo("/Male") : graphicPath.SubstringBackwardTo("/Female"));
			switch (gender)
			{
			case Gender.Female:
				pathGenderized = graphicPath.Replace("/Male", "/Female");
				break;
			case Gender.Male:
				pathGenderized = graphicPath.Replace("/Female", "/Male");
				break;
			}
		}
		if (Prefs.DevMode)
		{
			Log.Message("genderized  subpath=" + pathGenderized);
			Log.Message("non-genderized path=" + pathNonGenderized);
		}
	}

	internal static HashSet<HeadTypeDef> ReduceListByGender(Pawn pawn, HashSet<HeadTypeDef> l)
	{
		if (!pawn.HasStoryTracker() || l.NullOrEmpty())
		{
			return new HashSet<HeadTypeDef>();
		}
		if (Prefs.DevMode)
		{
			Log.Message("trying alternative method");
		}
		HashSet<HeadTypeDef> hashSet = new HashSet<HeadTypeDef>();
		string value = ((pawn.gender == Gender.Female) ? "Female" : "Male");
		foreach (HeadTypeDef item in l)
		{
			if (item.graphicPath.Contains(value))
			{
				hashSet.Add(item);
			}
		}
		if (Prefs.DevMode)
		{
			Log.Message("heads in list=" + l.Count() + " matching heads=" + hashSet.Count());
		}
		return hashSet;
	}

	internal static HashSet<HeadTypeDef> GetTestedHeadList(Pawn pawn, HashSet<HeadTypeDef> l, bool skipSkull = false)
	{
		if (!pawn.HasStoryTracker() || l.NullOrEmpty())
		{
			return new HashSet<HeadTypeDef>();
		}
		HashSet<HeadTypeDef> hashSet = new HashSet<HeadTypeDef>();
		foreach (HeadTypeDef item in l)
		{
			if (pawn.TestHead(item.graphicPath) && (!skipSkull || (!item.graphicPath.Contains("Skull") && !item.graphicPath.Contains("Stump"))))
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	internal static bool IsCoreHeadPath(string s)
	{
		return s == "Things/Pawn/Humanlike/Heads/" || s == "Things/Pawn/Humanlike/Heads";
	}

	internal static HashSet<HeadTypeDef> GetHeadDefList(this Pawn pawn, bool genderized = false)
	{
		if (!pawn.HasStoryTracker())
		{
			return null;
		}
		if (!pawn.kindDef.IsFromCoreMod() && pawn.IsAlienRace())
		{
			List<HeadTypeDef> list = pawn.AlienPartGenerator_GetHeadTypes();
			HashSet<HeadTypeDef> testedHeadList = GetTestedHeadList(pawn, list.ToHashSet());
			if (testedHeadList.Count > 0)
			{
				return testedHeadList;
			}
			if (list.Count > 0)
			{
				HashSet<HeadTypeDef> hashSet = ReduceListByGender(pawn, list.ToHashSet());
				if (hashSet.Count > 0)
				{
					return hashSet;
				}
				return list.ToHashSet();
			}
		}
		pawn.GetHeadPathFolderFor(out var pathG, out var pathNG);
		bool skipSkull = IsCoreHeadPath(pathNG) && CEditor.IsAlienRaceActive;
		HashSet<HeadTypeDef> hashSet2 = DefTool.ListBy((HeadTypeDef x) => x.graphicPath.Contains(pathG));
		HashSet<HeadTypeDef> hashSet3 = DefTool.ListBy((HeadTypeDef x) => x.graphicPath.Contains(pathNG));
		HashSet<HeadTypeDef> testedHeadList2 = GetTestedHeadList(pawn, hashSet2, skipSkull);
		HashSet<HeadTypeDef> testedHeadList3 = GetTestedHeadList(pawn, hashSet3, skipSkull);
		if (testedHeadList3.Count > 0)
		{
			return testedHeadList3;
		}
		if (testedHeadList2.Count > 0)
		{
			return testedHeadList2;
		}
		if (hashSet2.Count > 0)
		{
			return hashSet2;
		}
		if (hashSet3.Count > 0)
		{
			return hashSet3;
		}
		return DefTool.ListBy((HeadTypeDef x) => x.modContentPack == pawn.kindDef.modContentPack);
	}

	internal static string GetHeadName(this Pawn pawn, string path = null)
	{
		string text = "";
		if (pawn == null)
		{
			return "";
		}
		path = path ?? pawn?.story?.headType.graphicPath;
		path = path ?? "";
		if (path.Contains("Female"))
		{
			text += "♀ ";
		}
		else if (path.Contains("Male"))
		{
			text += "♂ ";
		}
		if (path.Contains("Average"))
		{
			text += "Average";
		}
		else if (path.Contains("Narrow"))
		{
			text += "Narrow";
		}
		text = (path.Contains("Wide") ? (text + " Wide") : (path.Contains("Pointy") ? (text + " Pointy") : ((!path.Contains("Normal")) ? (text + ((path.EndsWith("Average") || path.EndsWith("Narrow")) ? "" : path.SubstringBackwardFrom("_", withoutIt: false))) : (text + " Normal"))));
		if (text.Contains("/"))
		{
			text = text.SubstringBackwardFrom("/");
		}
		return text;
	}

	internal static string GetHeadTypeDefName(this Pawn p)
	{
		return (p != null && p.story != null && p.story.headType != null) ? p.story.headType.defName : "";
	}

	internal static void SetHeadTypeDef(this Pawn p, string defName)
	{
		if (p != null && p.story != null && !defName.NullOrEmpty())
		{
			p.SetHeadTypeDef(DefTool.GetDef<HeadTypeDef>(defName));
		}
	}

	internal static void SetHeadTypeDef(this Pawn p, HeadTypeDef def)
	{
		if (p.HasStoryTracker() && def != null)
		{
			p.story.headType = def;
			p.SetDirty();
		}
	}

	internal static bool SetHead(this Pawn pawn, bool next, bool random)
	{
		if (pawn == null)
		{
			return false;
		}
		if (pawn.story == null)
		{
			return true;
		}
		HashSet<HeadTypeDef> headDefList = pawn.GetHeadDefList();
		HeadTypeDef headType = pawn.story.headType;
		int index = headDefList.IndexOf(headType);
		index = headDefList.NextOrPrevIndex(index, next, random);
		HeadTypeDef headTypeDef = headDefList.ElementAt(index);
		if (Prefs.DevMode)
		{
			MessageTool.Show(headTypeDef.defName + " " + headTypeDef.modContentPack.Name);
		}
		pawn.SetHeadTypeDef(headTypeDef);
		return true;
	}

	internal static bool TestHead(this Pawn pawn, string headPath)
	{
		if (pawn == null || headPath.NullOrEmpty())
		{
			return false;
		}
		string text = headPath.Replace("\\", "/");
		bool flag = TextureTool.TestTexturePath(text + "_south", showError: false);
		if (!flag)
		{
			return false;
		}
		if (pawn.IsAlienRace())
		{
			bool flag2 = text.ToLower().Contains("/female");
			bool flag3 = text.ToLower().Contains("/male");
			if (flag2 || flag3)
			{
				flag = (pawn.gender == Gender.Female && flag2) || (pawn.gender == Gender.Male && flag3);
			}
			if (!flag2 && !flag3)
			{
				flag = true;
			}
		}
		return flag;
	}

	internal static Color GetEyeColor(this Pawn p)
	{
		return p.FA_GetControllerComp("EyeballControllerComp", alternative: true)?.GetMemberValue("color", Color.white) ?? Color.white;
	}

	internal static Color GetEyeColor2(this Pawn p)
	{
		return p.FA_GetControllerComp("EyeballControllerComp", alternative: true)?.GetMemberValue("secondColor", Color.white) ?? Color.white;
	}

	internal static void SetEyeColor(this Pawn p, Color col)
	{
		try
		{
			ThingComp thingComp = p.FA_GetControllerComp("EyeballControllerComp", alternative: true);
			if (thingComp != null)
			{
				Color color = default(Color);
				p.FA_SetDef("EyeballControllerComp", next: true, random: false);
				thingComp.SetMemberValue("color", color);
				thingComp.SetMemberValue("dirtyFlag", true);
				CEditor.API.UpdateGraphics();
				p.FA_SetDef("EyeballControllerComp", next: false, random: false);
				thingComp.SetMemberValue("color", col);
				thingComp.SetMemberValue("dirtyFlag", true);
				CEditor.API.UpdateGraphics();
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetEyeColor2(this Pawn p, Color col)
	{
		try
		{
			ThingComp thingComp = p.FA_GetControllerComp("EyeballControllerComp", alternative: true);
			if (thingComp != null)
			{
				Color color = default(Color);
				p.FA_SetDef("EyeballControllerComp", next: true, random: false);
				thingComp.SetMemberValue("color", color);
				thingComp.SetMemberValue("dirtyFlag", true);
				CEditor.API.UpdateGraphics();
				p.FA_SetDef("EyeballControllerComp", next: false, random: false);
				thingComp.SetMemberValue("secondColor", col);
				thingComp.SetMemberValue("dirtyFlag", true);
				CEditor.API.UpdateGraphics();
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
