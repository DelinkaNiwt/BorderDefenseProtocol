using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class IdeoTool
{
	internal static bool HasIdeoTracker(this Pawn p)
	{
		return p != null && p.ideo != null;
	}

	internal static string GetPawnCultureDefName(this Pawn pawn)
	{
		if (pawn.HasIdeoTracker() && pawn.Ideo != null)
		{
			return (pawn.Ideo.culture != null) ? pawn.Ideo.culture.defName : "";
		}
		return "";
	}

	internal static string GetPawnIdeoName(this Pawn pawn)
	{
		if (pawn.HasIdeoTracker() && pawn.Ideo != null)
		{
			return pawn.Ideo.name;
		}
		return "";
	}

	internal static void SetPawnIdeo(this Pawn pawn, string cultureDefName, string ideoName)
	{
		if (!pawn.HasIdeoTracker())
		{
			return;
		}
		try
		{
			List<Ideo> ideosListForReading = Find.IdeoManager.IdeosListForReading;
			foreach (Ideo item in ideosListForReading)
			{
				if (item.culture.defName == cultureDefName && item.name == ideoName)
				{
					pawn.ideo.SetIdeo(item);
					return;
				}
			}
			foreach (Ideo item2 in ideosListForReading)
			{
				if (item2.culture.defName == cultureDefName)
				{
					pawn.ideo.SetIdeo(item2);
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
