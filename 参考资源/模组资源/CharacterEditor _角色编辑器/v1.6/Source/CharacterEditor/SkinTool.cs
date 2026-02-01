using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class SkinTool
{
	internal static string GetFavColorDefName(this Pawn p)
	{
		return (p.HasStoryTracker() && p.story.favoriteColor != null) ? p.story.favoriteColor.defName : "";
	}

	internal static void SetFavColorDepricated(this Pawn p, Color col)
	{
	}

	internal static void SetFavColor(this Pawn pawn, ColorDef col)
	{
		if (pawn.HasStoryTracker())
		{
			pawn.story.favoriteColor = col;
		}
	}

	internal static void SetFavColorByDefName(this Pawn p, string defName)
	{
		if (!p.HasStoryTracker() || defName.NullOrEmpty())
		{
			return;
		}
		try
		{
			ColorDef col = DefTool.ColorDef(defName);
			p.SetFavColor(col);
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static Color GetFavColorDepricated(this Pawn pawn)
	{
		return (pawn.GetFavColor() == null) ? Color.white : pawn.GetFavColor().color;
	}

	internal static ColorDef GetFavColor(this Pawn pawn)
	{
		if (pawn.HasStoryTracker())
		{
			return pawn.story.favoriteColor;
		}
		return null;
	}

	internal static void SetMelanin(this Pawn pawn, float f)
	{
		if (pawn == null || pawn.story == null || f <= 0f)
		{
			return;
		}
		try
		{
			if (Prefs.DevMode)
			{
				Log.Message("initialising genes from old save by melanin " + f);
			}
			pawn.genes.InitializeGenesFromOldSave(f);
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static Color GetSkinColor(this Pawn p, bool primary)
	{
		if (p.IsAlienRace())
		{
			return p.AlienRaceComp_GetSkinColor(primary);
		}
		if (p.HasStoryTracker())
		{
			return p.story.SkinColor;
		}
		return Color.white;
	}

	internal static void SetSkinColor(this Pawn p, bool primary, Color color, bool ensureVisible = false)
	{
		try
		{
			if (!ensureVisible || (!(color.a <= 0f) && (!(color.r <= 0f) || !(color.g <= 0f) || !(color.b <= 0f))))
			{
				if (p.IsAlienRace())
				{
					p.AlienRaceComp_SetSkinColor(primary, color);
				}
				else if (p.HasStoryTracker())
				{
					p.story.skinColorOverride = color;
				}
				if (CEditor.IsFacialAnimationActive)
				{
					p.FA_SetDef("HeadControllerComp", next: false, random: false);
					p.SetEyeColor(CEditor.API.Pawn.GetEyeColor());
					p.FA_SetDef("HeadControllerComp", next: true, random: false);
					p.SetEyeColor(CEditor.API.Pawn.GetEyeColor());
				}
				p.SetDirty();
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
