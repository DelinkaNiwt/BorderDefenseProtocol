using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "GetLetterText")]
public static class IncidentWorker_RaidEnemy_GetLetterText_Patch
{
	private static bool Prefix(ref string __result, IncidentParms parms, List<Pawn> pawns)
	{
		if (parms.raidStrategy.Worker is RaidStrategyWorker_ImmediateAttack_Psycasters)
		{
			string text = string.Format(parms.raidArrivalMode.textEnemy, parms.faction.def.pawnsPlural, parms.faction.Name.ApplyTag(parms.faction)).CapitalizeFirst();
			text += "\n\n";
			text += parms.raidStrategy.arrivalTextEnemy;
			List<Pawn> source = pawns.Where((Pawn x) => x.HasPsylink).ToList();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string item in source.Select((Pawn x) => x.Name?.ToString() + " - " + x.KindLabel))
			{
				stringBuilder.AppendLine(item);
			}
			text += "VPE.PsycasterRaidDescription".Translate(stringBuilder.ToString());
			Pawn pawn = pawns.Find((Pawn x) => x.Faction.leader == x);
			if (pawn != null)
			{
				text += "\n\n";
				text += "EnemyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER"));
			}
			__result = text;
			return false;
		}
		return true;
	}
}
