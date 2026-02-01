using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class HairTool
{
	internal const string CO_SETTINGS = "settings";

	internal const string CO_MASK = "mask";

	internal const string CO_ENABLED = "enabled";

	internal const string CO_COMPGRADIENTHAIR = "CompGradientHair";

	internal const string CO_GRADIENTHAIR = "GradientHair";

	internal const string CO_GRADIENTHAIRSLASH = "GradientHair/";

	internal const string CO_HAIRCOLOR = "hairColor";

	internal const string CO_REF_GRADIENTHAIR = "GradientHair";

	internal const string CO_METHODSETGRADIENTHAIR = "SetGradientHair";

	internal const string CO_METHODGETGRADIENTHAIR = "GetGradientHair";

	internal const string CO_PUBLICAPI = "PublicApi";

	internal static string selectedHairModName = null;

	internal static HashSet<HairDef> lOfHairDefs = null;

	internal static bool isHairConfigOpen = true;

	internal static bool onMouseover = false;

	internal static Color GetHairColor(this Pawn p, bool primary)
	{
		return (!p.HasStoryTracker()) ? Color.white : (primary ? p.story.HairColor : p.GetGradientColor());
	}

	internal static void SetGradientMask(this Pawn p, string mask)
	{
		if (p == null || mask.NullOrEmpty())
		{
			return;
		}
		try
		{
			ThingComp gradientComp = p.GetGradientComp();
			if (gradientComp != null)
			{
				object memberValue = gradientComp.GetMemberValue<object>("settings", null);
				if (memberValue != null)
				{
					memberValue.SetMemberValue("mask", mask);
					memberValue.SetMemberValue("enabled", true);
				}
				CEditor.API.UpdateGraphics();
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static ThingComp GetGradientComp(this Pawn p)
	{
		if (p == null || p.AllComps.NullOrEmpty() || !CEditor.IsGradientHairActive)
		{
			return null;
		}
		foreach (ThingComp allComp in p.AllComps)
		{
			if (allComp.GetType().ToString().EndsWith("CompGradientHair"))
			{
				return allComp;
			}
		}
		return null;
	}

	internal static List<string> GetAllGradientHairs()
	{
		List<Texture2D> list = ContentFinder<Texture2D>.GetAllInFolder("GradientHair").ToList();
		HashSet<Texture2D> hashSet = new HashSet<Texture2D>();
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (Texture2D item in list)
		{
			if (!hashSet2.Contains(item.name))
			{
				hashSet.Add(item);
				hashSet2.Add(item.name);
			}
		}
		return (from t in hashSet
			orderby t.name
			select "GradientHair/" + t.name).ToList();
	}

	internal static void RandomizeGradientMask(this Pawn p)
	{
		if (p != null)
		{
			List<string> allGradientHairs = GetAllGradientHairs();
			string mask = allGradientHairs.RandomElement();
			p.SetGradientMask(mask);
		}
	}

	internal static string GetGradientMask(this Pawn p)
	{
		if (p == null)
		{
			return "";
		}
		ThingComp gradientComp = p.GetGradientComp();
		if (gradientComp != null)
		{
			object memberValue = gradientComp.GetMemberValue<object>("settings", null);
			if (memberValue != null)
			{
				return memberValue.GetMemberValue("mask", "");
			}
			return "";
		}
		return "";
	}

	private static Color GetGradientColor(this Pawn p)
	{
		if (CEditor.IsGradientHairActive)
		{
			object[] array = new object[3] { p, null, null };
			Reflect.GetAType("GradientHair", "PublicApi").CallMethod("GetGradientHair", array);
			return (Color)array[2];
		}
		return Color.white;
	}

	internal static string GetHairDefName(this Pawn p)
	{
		return (p.HasStoryTracker() && p.story.hairDef != null) ? p.story.hairDef.defName : "";
	}

	internal static string GetHairName(this Pawn p)
	{
		return (p.HasStoryTracker() && p.story.hairDef != null) ? p.story.hairDef.LabelCap.ToString() : "";
	}

	internal static HashSet<HairDef> GetHairList(string modname)
	{
		return (from hr in DefTool.ListByMod<HairDef>(modname)
			orderby !hr.noGraphic
			select hr).ToHashSet();
	}

	internal static int GetHairDefCount(string modname)
	{
		return DefTool.ListByMod<HairDef>(modname).ToList().CountAllowNull();
	}

	internal static HairDef GetRandomHairDef(string modname)
	{
		List<HairDef> source = DefTool.ListByMod<HairDef>(modname).ToList();
		return source.RandomElement();
	}

	internal static void SetHair(this Pawn p, HairDef h)
	{
		if (p.HasStoryTracker() && h != null)
		{
			p.story.hairDef = h;
			p.SetDirty();
		}
	}

	internal static void SetHair(this Pawn pawn, bool next, bool random, string modname = null)
	{
		if (pawn != null && pawn.story != null)
		{
			List<HairDef> list = DefTool.ListByMod<HairDef>(modname).ToList();
			int index = list.IndexOf(pawn.story.hairDef);
			index = list.NextOrPrevIndex(index, next, random);
			pawn.SetHair(list[index]);
		}
	}

	internal static void SetHairColor(this Pawn p, bool primary, Color col)
	{
		if (!p.HasStoryTracker())
		{
			return;
		}
		try
		{
			if (primary)
			{
				p.story.SetMemberValue("hairColor", col);
			}
			else
			{
				p.SetGradientHairColor(col);
			}
			if (p.IsAlienRace())
			{
				p.AlienRaceComp_SetHairColor(primary, col);
			}
			p.SetDirty();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	private static void SetGradientHairColor(this Pawn pawn, Color colorB)
	{
		if (CEditor.IsGradientHairActive)
		{
			object[] param = new object[3] { pawn, true, colorB };
			Reflect.GetAType("GradientHair", "PublicApi").CallMethod("SetGradientHair", param);
		}
	}

	internal static void AChooseHairCustom()
	{
		SZWidgets.FloatMenuOnRect(GetHairList(null), (HairDef s) => s.LabelCap, ASetHairCustom);
	}

	internal static void ASetHairCustom(HairDef hairDef)
	{
		CEditor.API.Pawn.SetHair(hairDef);
		CEditor.API.UpdateGraphics();
	}

	internal static void ARandomHair()
	{
		CEditor.API.Pawn.SetHair(next: true, random: true, selectedHairModName);
		if (Event.current.alt)
		{
			CEditor.API.Pawn.SetHairColor(primary: true, ColorTool.RandomColor);
			CEditor.API.Pawn.SetHairColor(primary: false, ColorTool.RandomColor);
		}
		else if (Event.current.control)
		{
			CEditor.API.Pawn.SetHairColor(primary: true, ColorTool.RandomAlphaColor);
			CEditor.API.Pawn.SetHairColor(primary: false, ColorTool.RandomAlphaColor);
		}
		CEditor.API.UpdateGraphics();
	}

	internal static void ASelectedHairModName(string val)
	{
		selectedHairModName = val;
		lOfHairDefs = GetHairList(selectedHairModName);
	}

	internal static void AConfigHair()
	{
		isHairConfigOpen = !isHairConfigOpen;
	}
}
