using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public static class CaravanPawnUtility
{
	public static void FindColonistWithBestSkill(Caravan caravan, SkillDef skillDef, out Pawn pawn, out int level)
	{
		level = 0;
		if (caravan == null || caravan.PawnsListForReading.NullOrEmpty())
		{
			pawn = null;
			return;
		}
		pawn = (from p in caravan.PawnsListForReading
			where IsConsciousOwner(p, caravan) && p.kindDef.RaceProps.Humanlike && p.skills.GetSkill(skillDef) != null
			orderby p.skills.GetSkill(skillDef).Level descending
			select p).FirstOrDefault();
		if (pawn != null && pawn.skills.GetSkill(skillDef) != null)
		{
			level = pawn.skills.GetSkill(skillDef).Level;
		}
	}

	public static void ColonistsLearnSkill(Caravan caravan, SkillDef skillDef, float xp, bool direct = false)
	{
		List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
		for (int i = 0; i < pawnsListForReading.Count; i++)
		{
			Pawn pawn = pawnsListForReading[i];
			if (pawn.skills?.GetSkill(skillDef) != null)
			{
				pawn.skills.Learn(skillDef, xp, direct);
			}
		}
	}

	private static bool IsConsciousOwner(Pawn pawn, Caravan caravan)
	{
		if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState)
		{
			return caravan.IsOwner(pawn);
		}
		return false;
	}
}
