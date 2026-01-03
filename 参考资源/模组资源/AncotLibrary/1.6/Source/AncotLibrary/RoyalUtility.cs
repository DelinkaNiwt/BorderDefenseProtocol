using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class RoyalUtility
{
	public static bool TitleSenioritySatisfied(Pawn pawn, Faction faction, int seniorityRequired)
	{
		if (pawn == null)
		{
			return false;
		}
		if (pawn.Dead || !pawn.Faction.IsPlayer)
		{
			return false;
		}
		if (pawn.royalty != null && pawn.royalty.HasAnyTitleIn(faction) && pawn.royalty.GetCurrentTitle(faction).seniority >= seniorityRequired)
		{
			return true;
		}
		return false;
	}

	public static bool CanUpdateTitle(Pawn pawn, Faction faction)
	{
		return (!pawn.IsMutant || !pawn.mutant.Def.disableTitles) && pawn.Faction != null && pawn.Faction.IsPlayer && pawn.royalty != null && pawn.royalty.CanUpdateTitle(faction);
	}

	public static bool CanUpdateTitleForPawns(List<Pawn> pawns, out List<Pawn> pawnsForTitle, Faction faction)
	{
		pawnsForTitle = new List<Pawn>();
		foreach (Pawn pawn in pawns)
		{
			if (CanUpdateTitle(pawn, faction))
			{
				pawnsForTitle.Add(pawn);
			}
		}
		return pawnsForTitle.Count > 0;
	}
}
