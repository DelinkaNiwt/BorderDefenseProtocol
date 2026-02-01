using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class RelationTool
{
	internal static bool AreThosePawnSisBro(Pawn a, Pawn b)
	{
		Pawn commonParent = GetCommonParent(a, b, Gender.Male);
		Pawn commonParent2 = GetCommonParent(a, b, Gender.Female);
		return commonParent != null && commonParent2 != null;
	}

	internal static Pawn GetCommonParent(Pawn a, Pawn b, Gender gender)
	{
		Pawn result = null;
		if (a == null || b == null || a.relations == null || b.relations == null)
		{
			return result;
		}
		foreach (DirectPawnRelation directRelation in a.relations.DirectRelations)
		{
			if (directRelation.def != PawnRelationDefOf.Parent)
			{
				continue;
			}
			foreach (DirectPawnRelation directRelation2 in b.relations.DirectRelations)
			{
				if (directRelation2.def == PawnRelationDefOf.Parent && directRelation.otherPawn == directRelation2.otherPawn && directRelation.otherPawn.gender == gender)
				{
					return result;
				}
			}
		}
		return null;
	}

	internal static Pawn GetFirstParentForPawn(this Pawn p, Gender g)
	{
		if (p == null)
		{
			return null;
		}
		foreach (Pawn relatedPawn in p.relations.RelatedPawns)
		{
			if (relatedPawn.gender == g)
			{
				DirectPawnRelation directRelation = p.relations.GetDirectRelation(PawnRelationDefOf.Parent, relatedPawn);
				if (directRelation != null)
				{
					return relatedPawn;
				}
			}
		}
		return null;
	}

	internal static List<Pawn> GetRelatedPawns(this Pawn p, out int countImpliedByOtherPawn)
	{
		countImpliedByOtherPawn = 0;
		List<Pawn> list = new List<Pawn>();
		if (!p.HasRelationTracker() || p.relations.RelatedPawns.EnumerableNullOrEmpty())
		{
			return list;
		}
		foreach (Pawn relatedPawn in p.relations.RelatedPawns)
		{
			list.Add(relatedPawn);
			foreach (PawnRelationDef item in p.GetRelations(relatedPawn).ToList())
			{
				if (item.implied)
				{
					countImpliedByOtherPawn++;
				}
			}
		}
		return list;
	}

	internal static string GetRelationAsSeparatedString(this DirectPawnRelation d)
	{
		if (d == null || d.def.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = text + d.def.defName + "|";
		text = text + d.otherPawn.GetPawnNameAsSeparatedString() + "|";
		return text + d.startTicks;
	}

	internal static string GetRelationsAsSeparatedString(this Pawn p)
	{
		if (!p.HasRelationTracker() || p.relations.DirectRelations.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (DirectPawnRelation directRelation in p.relations.DirectRelations)
		{
			text += directRelation.GetRelationAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static string RelationLabelDirect(this DirectPawnRelation dr)
	{
		if (dr == null || dr.otherPawn == null)
		{
			return "";
		}
		return ((dr.otherPawn.gender == Gender.Female) ? dr.def.labelFemale : dr.def.label) + " " + dr.otherPawn.GetPawnName(needFull: true);
	}

	internal static string RelationLabelIndirect(this PawnRelationDef prd, Pawn otherPawn)
	{
		if (prd == null || otherPawn == null)
		{
			return "";
		}
		return ((otherPawn.gender == Gender.Female) ? prd.labelFemale : prd.label) + " " + otherPawn.GetPawnName(needFull: true);
	}

	internal static string RelationTooltip(this Pawn pawn, Pawn otherPawn)
	{
		return otherPawn.GetPawnDescription(pawn) + "\n" + GetPawnRowTooltip(otherPawn, pawn);
	}

	internal static void SetRelationsFromSeparatedString(this Pawn p, string s)
	{
		if (s.NullOrEmpty() || !p.HasRelationTracker())
		{
			return;
		}
		try
		{
			string[] array = s.SplitNo(":");
			p.relations.ClearAllRelations();
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length != 3)
				{
					continue;
				}
				PawnRelationDef pawnRelationDef = DefTool.PawnRelationDef(array3[0]);
				if (pawnRelationDef == null)
				{
					continue;
				}
				Pawn otherPawnFromSeparatedString = PawnxTool.GetOtherPawnFromSeparatedString(array3[1]);
				if (otherPawnFromSeparatedString == null)
				{
					continue;
				}
				p.relations.AddDirectRelation(pawnRelationDef, otherPawnFromSeparatedString);
				if (p.relations.DirectRelationExists(pawnRelationDef, otherPawnFromSeparatedString))
				{
					DirectPawnRelation directPawnRelation = p.relations.DirectRelations.Last();
					if (directPawnRelation.def.defName == pawnRelationDef.defName && directPawnRelation.otherPawn == otherPawnFromSeparatedString)
					{
						directPawnRelation.startTicks = array3[2].AsInt32();
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	private static string GetPawnRowTooltip(Pawn otherPawn, Pawn pawn)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (otherPawn.RaceProps.Humanlike && pawn.RaceProps.Humanlike)
		{
			stringBuilder.AppendLine(pawn.relations.OpinionExplanation(otherPawn));
			stringBuilder.AppendLine();
			stringBuilder.Append("SomeonesOpinionOfMe".Translate(otherPawn.LabelShort, otherPawn));
			stringBuilder.Append(": ");
			stringBuilder.Append(otherPawn.relations.OpinionOf(pawn).ToStringWithSign());
		}
		else
		{
			stringBuilder.AppendLine(otherPawn.LabelCapNoCount);
			string pawnSituationLabel = SocialCardUtility.GetPawnSituationLabel(otherPawn, pawn);
			if (!pawnSituationLabel.NullOrEmpty())
			{
				stringBuilder.AppendLine(pawnSituationLabel);
			}
			stringBuilder.AppendLine("--------------");
			string text = "";
			if (otherPawn.relations.DirectRelations.Count == 0)
			{
				if (otherPawn.relations.OpinionOf(pawn) < -20)
				{
					return "Rival".Translate();
				}
				return (otherPawn.relations.OpinionOf(pawn) > 20) ? ((string)"Friend".Translate()) : ((string)"Acquaintance".Translate());
			}
			for (int i = 0; i < otherPawn.relations.DirectRelations.Count; i++)
			{
				PawnRelationDef def = otherPawn.relations.DirectRelations[i].def;
				text = (text.NullOrEmpty() ? def.GetGenderSpecificLabelCap(otherPawn) : (text + ", " + def.GetGenderSpecificLabel(otherPawn)));
			}
			stringBuilder.Append(text);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Compatibility: " + pawn.relations.CompatibilityWith(otherPawn).ToString("F2"));
		stringBuilder.Append("RomanceChanceFactor: " + pawn.relations.SecondaryRomanceChanceFactor(otherPawn).ToString("F2"));
		return stringBuilder.ToString();
	}
}
