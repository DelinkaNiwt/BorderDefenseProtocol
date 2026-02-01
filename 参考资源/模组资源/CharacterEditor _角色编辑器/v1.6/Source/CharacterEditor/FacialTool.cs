using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CharacterEditor;

internal static class FacialTool
{
	internal const string CO_FACIALANIMATION = "FacialAnimation";

	internal const string CO_FACE = "HeadControllerComp";

	internal const string CO_EYE = "EyeballControllerComp";

	internal const string CO_LID = "LidControllerComp";

	internal const string CO_BROW = "BrowControllerComp";

	internal const string CO_MOUTH = "MouthControllerComp";

	internal const string CO_SKIN = "SkinControllerComp";

	internal const string CO_FACETYPE = "faceType";

	internal const string CO_COLOR = "color";

	internal const string CO_SECONDCOLOR = "secondColor";

	internal const string CO_DIRTYFLAG = "dirtyFlag";

	internal const string CO_SPM1 = "SPM1";

	internal const string CO_EYEBALL = "Eyeball";

	internal const string CO_HEADPOINTY = "HeadPointy";

	internal const string CO_HEADSQUARE = "HeadSquare";

	internal const string CO_EYEDULL = "EyeDull";

	internal const string CO_LIDPOINTY = "LidPointy";

	internal const string CO_LIDFLASHY = "LidFlashy";

	internal const string CO_LIDQUITE = "LidQuite";

	internal const string CO_LIDSLEEPY = "LidSleepy";

	internal const string CO_RJW = "rjw";

	internal const string CO_COMPRJW = "CompRJW";

	internal const string CO_COMPENNEAGRAM = "CompEnneagram";

	internal static string GetFacialAnimationParams(this Pawn p)
	{
		if (p == null || !CEditor.IsFacialAnimationActive)
		{
			return "";
		}
		string text = "";
		text = text + p.FA_GetCurrentDefName("HeadControllerComp") + "|";
		text = text + p.FA_GetCurrentDefName("EyeballControllerComp") + "|";
		text = text + p.FA_GetCurrentDefName("LidControllerComp") + "|";
		text = text + p.FA_GetCurrentDefName("BrowControllerComp") + "|";
		text = text + p.FA_GetCurrentDefName("MouthControllerComp") + "|";
		return text + p.FA_GetCurrentDefName("SkinControllerComp");
	}

	internal static void SetFacialAnimationParams(this Pawn p, string s)
	{
		if (p == null || !CEditor.IsFacialAnimationActive || s.NullOrEmpty())
		{
			return;
		}
		try
		{
			string[] array = s.SplitNo("|");
			if (array.Length >= 6)
			{
				p.FA_SetDefByName("HeadControllerComp", array[0].Trim());
				p.FA_SetDefByName("EyeballControllerComp", array[1].Trim());
				p.FA_SetDefByName("LidControllerComp", array[2].Trim());
				p.FA_SetDefByName("BrowControllerComp", array[3].Trim());
				p.FA_SetDefByName("MouthControllerComp", array[4].Trim());
				p.FA_SetDefByName("SkinControllerComp", array[5].Trim());
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static ThingComp FA_GetControllerComp(this Pawn p, string controller, bool alternative = false)
	{
		if (p == null || p.AllComps.NullOrEmpty() || !CEditor.IsFacialAnimationActive)
		{
			return null;
		}
		foreach (ThingComp allComp in p.AllComps)
		{
			if (allComp.GetType().ToString().StartsWith("FacialAnimation"))
			{
				if (alternative && controller == "EyeballControllerComp" && allComp.GetType().ToString().Contains("Eyeball"))
				{
					return allComp;
				}
				if (allComp.GetType().ToString().EndsWith(controller))
				{
					return allComp;
				}
			}
		}
		return null;
	}

	internal static string FA_GetCurrentDefName(this Pawn p, string controller)
	{
		object obj = p.FA_GetCurrentDef(controller);
		return (obj != null) ? ((Def)obj).defName : "";
	}

	internal static object FA_GetCurrentDef(this Pawn p, string controller)
	{
		return p.FA_GetControllerComp(controller)?.GetMemberValue<object>("faceType", null);
	}

	internal static List<string> FA_GetDefStringList(this Pawn p, string controller)
	{
		List<string> list = new List<string>();
		object obj = p.FA_GetCurrentDef(controller);
		if (obj != null)
		{
			List<Def> list2 = FA_FilterOutIncompatible(p, controller, FA_GetDefList(obj));
			if (list2 != null)
			{
				foreach (Def item in list2)
				{
					list.Add(item.defName);
				}
			}
		}
		return list;
	}

	internal static List<Def> FA_GetDefList(object curDef)
	{
		return GenDefDatabase.GetAllDefsInDatabaseForDef(curDef.GetType()).ToList();
	}

	internal static List<Def> FA_FilterOutIncompatible(Pawn p, string controller, List<Def> l)
	{
		if (p == null || l == null || l.Count == 1)
		{
			return l;
		}
		List<Def> list = new List<Def>();
		switch (controller)
		{
		case "HeadControllerComp":
			if (p.gender == Gender.Male)
			{
				foreach (Def item in l)
				{
					if (item.defName != "HeadPointy")
					{
						list.Add(item);
					}
				}
			}
			else
			{
				foreach (Def item2 in l)
				{
					if (item2.defName != "HeadSquare")
					{
						list.Add(item2);
					}
				}
			}
			return list;
		case "EyeballControllerComp":
			if (p.gender == Gender.Male)
			{
				foreach (Def item3 in l)
				{
					if (item3.defName != "EyeDull")
					{
						list.Add(item3);
					}
				}
				return list;
			}
			return l;
		case "LidControllerComp":
			if (p.gender == Gender.Male)
			{
				foreach (Def item4 in l)
				{
					if (item4.defName != "LidPointy" && item4.defName != "LidFlashy" && item4.defName != "LidQuite" && item4.defName != "LidSleepy")
					{
						list.Add(item4);
					}
				}
				return list;
			}
			return l;
		default:
			return l;
		}
	}

	internal static Def FA_GetDefByName(Pawn p, string controller, string defName)
	{
		object obj = p.FA_GetCurrentDef(controller);
		if (obj == null)
		{
			return null;
		}
		List<Def> source = FA_FilterOutIncompatible(p, controller, FA_GetDefList(obj));
		return source.Where((Def td) => td.defName == defName).FirstOrDefault();
	}

	internal static void FA_SetDefByName(this Pawn p, string controller, string defName)
	{
		ThingComp thingComp = p.FA_GetControllerComp(controller);
		if (thingComp != null)
		{
			thingComp.SetMemberValue("faceType", FA_GetDefByName(p, controller, defName));
			thingComp.PostExposeData();
			CEditor.API.UpdateGraphics();
		}
	}

	internal static bool FA_SetDef(this Pawn p, string controller, bool next, bool random, bool keep = false)
	{
		ThingComp thingComp = p.FA_GetControllerComp(controller);
		if (thingComp != null)
		{
			object memberValue = thingComp.GetMemberValue<object>("faceType", null);
			if (memberValue != null)
			{
				string defName = ((Def)memberValue).defName;
				List<Def> list = FA_FilterOutIncompatible(p, controller, FA_GetDefList(memberValue));
				if (list != null)
				{
					int index = 0;
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].defName == defName)
						{
							index = i;
							break;
						}
					}
					if (!keep)
					{
						index = list.NextOrPrevIndex(index, next, random);
					}
					thingComp.SetMemberValue("faceType", list[index]);
					return true;
				}
			}
			thingComp.SetMemberValue("dirtyFlag", true);
			thingComp.PostExposeData();
			CEditor.API.UpdateGraphics();
		}
		return false;
	}

	internal static ThingComp GetRJWComp(this Pawn p)
	{
		if (p == null || p.AllComps.NullOrEmpty() || !CEditor.IsRJWActive)
		{
			return null;
		}
		foreach (ThingComp allComp in p.AllComps)
		{
			if (allComp.GetType().ToString().StartsWith("rjw") && allComp.GetType().ToString().EndsWith("CompRJW"))
			{
				return allComp;
			}
		}
		return null;
	}

	internal static ThingComp GetPersoComp(this Pawn p)
	{
		if (p == null || p.AllComps.NullOrEmpty() || !CEditor.IsPersonalitiesActive)
		{
			return null;
		}
		foreach (ThingComp allComp in p.AllComps)
		{
			if (allComp.GetType().ToString().StartsWith("SPM1") && allComp.GetType().ToString().EndsWith("CompEnneagram"))
			{
				return allComp;
			}
		}
		return null;
	}
}
