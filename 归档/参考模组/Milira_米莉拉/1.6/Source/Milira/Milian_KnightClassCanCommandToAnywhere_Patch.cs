using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(MechanitorUtility))]
[HarmonyPatch("InMechanitorCommandRange")]
public static class Milian_KnightClassCanCommandToAnywhere_Patch
{
	[HarmonyPostfix]
	public static bool Prefix(Pawn mech, LocalTargetInfo target, ref bool __result)
	{
		if (InPawnIVCommandRange(mech, target))
		{
			__result = true;
			return false;
		}
		if (mech.def.defName == "Milian_Mechanoid_KnightI" || mech.def.defName == "Milian_Mechanoid_KnightII" || mech.def.defName == "Milian_Mechanoid_KnightIII" || mech.def.defName == "Milian_Mechanoid_KnightIV" || mech.def.defName == "Milian_Mechanoid_BishopIV" || mech.def.defName == "Milian_Mechanoid_PawnIV" || mech.def.defName == "Milian_Mechanoid_RookIII")
		{
			if (mech.IsPlayerControlled)
			{
				__result = true;
				return false;
			}
			return true;
		}
		if (ModsConfig.IsActive("Ancot.MilianModification") && mech.health.hediffSet.hediffs.Any((Hediff d) => d.def == MiliraDefOf.MilianFitting_Dejitterizer))
		{
			__result = true;
			return false;
		}
		return true;
	}

	public static bool InPawnIVCommandRange(Pawn mech, LocalTargetInfo target)
	{
		Map currentMap = Find.CurrentMap;
		List<Pawn> list = currentMap.mapPawns.SpawnedColonyMechs.Where((Pawn p) => p.TryGetComp<CompMechCommandRadius>() != null).ToList();
		if (!list.NullOrEmpty())
		{
			foreach (Pawn item in list)
			{
				if (mech.MapHeld != item.MapHeld)
				{
					return false;
				}
				CompMechCommandRadius compMechCommandRadius = item.TryGetComp<CompMechCommandRadius>();
				if (!item.Downed && compMechCommandRadius.CanCommandTo(item, target))
				{
					if (ModsConfig.IsActive("Ancot.MilianModification") && item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_PathSynchronizer) != null && item != mech)
					{
						HealthUtility.AdjustSeverity(mech, MiliraDefOf.Milian_PathPlanning, 1f);
					}
					return true;
				}
			}
		}
		if (ModsConfig.IsActive("Ancot.MilianModification"))
		{
			Hediff firstHediffOfDef = mech.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_PathPlanning);
			if (firstHediffOfDef != null)
			{
				mech.health.RemoveHediff(firstHediffOfDef);
			}
		}
		return false;
	}

	public static bool CanCommandTo(Pawn pawn, LocalTargetInfo target)
	{
		if (!target.Cell.InBounds(pawn.MapHeld))
		{
			return false;
		}
		return (float)pawn.Position.DistanceToSquared(target.Cell) < 620.01f;
	}
}
