using System;
using System.Linq;
using System.Text;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalk.Service;

public static class RelationsService
{
	private const float FriendOpinionThreshold = 20f;

	private const float RivalOpinionThreshold = -20f;

	public static string GetRelationsString(Pawn pawn)
	{
		if (pawn?.relations == null)
		{
			return "";
		}
		StringBuilder relationsSb = new StringBuilder();
		foreach (Pawn otherPawn in PawnSelector.GetAllNearByPawns(pawn).Take(Settings.Get().Context.MaxPawnContextCount - 1))
		{
			if (otherPawn == pawn || (!otherPawn.RaceProps.Humanlike && !otherPawn.HasVocalLink()) || otherPawn.Dead)
			{
				continue;
			}
			Pawn_RelationsTracker relations = otherPawn.relations;
			if (relations != null && relations.hidePawnRelations)
			{
				continue;
			}
			string label = null;
			try
			{
				float opinionValue = pawn.relations.OpinionOf(otherPawn);
				PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(otherPawn);
				if (mostImportantRelation != null)
				{
					label = mostImportantRelation.GetGenderSpecificLabelCap(otherPawn);
				}
				if (string.IsNullOrEmpty(label))
				{
					label = GetStatusLabel(pawn, otherPawn);
				}
				if (string.IsNullOrEmpty(label) && !pawn.IsVisitor() && !pawn.IsEnemy())
				{
					label = ((opinionValue >= 20f) ? ((string)"Friend".Translate()) : ((!(opinionValue <= -20f)) ? ((string)"Acquaintance".Translate()) : ((string)"Rival".Translate())));
				}
				if (!string.IsNullOrEmpty(label))
				{
					string pawnName = otherPawn.LabelShort;
					string opinion = opinionValue.ToStringWithSign();
					relationsSb.Append(pawnName + "(" + label + ") " + opinion + ", ");
				}
			}
			catch (Exception)
			{
			}
		}
		if (relationsSb.Length > 0)
		{
			relationsSb.Length -= 2;
			return "Relations: " + relationsSb;
		}
		return "";
	}

	private static string GetStatusLabel(Pawn pawn, Pawn otherPawn)
	{
		if ((pawn.IsPrisoner || pawn.IsSlave) && otherPawn.IsFreeNonSlaveColonist)
		{
			return "Master".Translate();
		}
		if (otherPawn.IsPrisoner)
		{
			return "Prisoner".Translate();
		}
		if (otherPawn.IsSlave)
		{
			return "Slave".Translate();
		}
		if (pawn.Faction != null && otherPawn.Faction != null && pawn.Faction.HostileTo(otherPawn.Faction))
		{
			return "Enemy".Translate();
		}
		return null;
	}
}
