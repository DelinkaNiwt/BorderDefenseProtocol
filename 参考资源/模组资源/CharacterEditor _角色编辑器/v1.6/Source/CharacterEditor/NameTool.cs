using System;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class NameTool
{
	internal static string GetGenderInt(this Pawn pawn)
	{
		int gender = (int)pawn.gender;
		return gender.ToString();
	}

	internal static void SetGenderInt(this Pawn pawn, int gender)
	{
		if (pawn == null || pawn.kindDef == null)
		{
			return;
		}
		try
		{
			Gender gender2 = (Gender)gender;
			if ((pawn.kindDef.RaceProps.hasGenders && gender2 != Gender.None) || (!pawn.kindDef.RaceProps.hasGenders && gender2 == Gender.None))
			{
				pawn.gender = gender2;
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetPawnGender(this Pawn pawn, Gender gender)
	{
		if (pawn != null)
		{
			Gender gender2 = pawn.gender;
			pawn.gender = gender;
			if (!pawn.SetHead(next: true, random: false))
			{
				pawn.gender = gender2;
			}
			CEditor.API.UpdateGraphics();
		}
	}

	internal static string GetPawnDescription(this Pawn pawn, Pawn otherPawn = null)
	{
		if (pawn == null)
		{
			return "";
		}
		string str = pawn.KindLabel ?? "";
		string text = pawn?.def?.LabelCap.ToString();
		text = text ?? "";
		str = str.CapitalizeFirst();
		text = text.CapitalizeFirst();
		if (str != text && !str.NullOrEmpty())
		{
			text += ", ";
			text += str;
		}
		text += ", ";
		text += pawn.gender.GetLabel(pawn.RaceProps.Animal).CapitalizeFirst();
		text += ", ";
		text += ((!pawn.HasAgeTracker()) ? "" : ((string)"AgeIndicator".Translate(pawn.ageTracker.AgeNumberString)));
		if (pawn.Faction != null)
		{
			text += ", ";
			text += pawn.Faction.Name.CapitalizeFirst().Colorize(pawn.Faction.Color);
			if (otherPawn != null && otherPawn.Faction != null)
			{
				FactionRelationKind kind = ((pawn.Faction == otherPawn.Faction) ? FactionRelationKind.Ally : pawn.Faction.RelationKindWith(otherPawn.Faction));
				text = text + " " + (((pawn.Faction == otherPawn.Faction) ? 100 : pawn.Faction.GoodwillWith(otherPawn.Faction)) + " " + kind.GetLabel() + "\n").Colorize(kind.GetColor());
			}
		}
		return text;
	}

	internal static string GetRoyalTitleAsSeparatedString(this Pawn p)
	{
		if (!p.HasRoyaltyTracker())
		{
			return "";
		}
		string result = "";
		if (!p.royalty.MainTitle().IsNullOrEmpty())
		{
			result = p.royalty.MainTitle().defName;
		}
		return result;
	}

	internal static void SetRoyalTitleFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasRoyaltyTracker())
		{
			return;
		}
		try
		{
			if (!p.royalty.AllTitlesForReading.NullOrEmpty())
			{
				foreach (RoyalTitle item in p.royalty.AllTitlesForReading)
				{
					p.RemoveTitle(item);
				}
			}
			if (!s.NullOrEmpty())
			{
				p.SetTitle(s);
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetTitle(this Pawn pawn, string defName)
	{
		if (!pawn.HasRoyaltyTracker())
		{
			return;
		}
		RoyalTitleDef def = DefTool.GetDef<RoyalTitleDef>(defName);
		if (def != null)
		{
			Faction faction = Find.FactionManager.AllFactions.Where((Faction f) => f.def.RoyalTitlesAwardableInSeniorityOrderForReading.Count > 0).ToList().RandomElement();
			pawn.royalty.SetTitle(faction, def, grantRewards: true);
		}
	}

	internal static string GetMainTitle(this Pawn pawn)
	{
		return pawn.HasRoyalTitle() ? pawn.royalty.MainTitle().GetLabelCapFor(pawn) : "";
	}

	internal static void RemoveTitle(this Pawn pawn, RoyalTitle title)
	{
		RoyalTitleUtility.FindLostAndGainedPermits(title.def, null, out var _, out var lostPermits);
		StringBuilder stringBuilder = new StringBuilder();
		if (lostPermits.Count > 0)
		{
			stringBuilder.AppendLine("RenounceTitleWillLoosePermits".Translate(pawn.Named("PAWN")) + ":");
			foreach (RoyalTitlePermitDef item in lostPermits)
			{
				stringBuilder.AppendLine("- " + item.LabelCap + " (" + FirstTitleWithPermit(item).GetLabelFor(pawn) + ")");
			}
			stringBuilder.AppendLine();
		}
		if (!title.faction.def.renounceTitleMessage.NullOrEmpty())
		{
			stringBuilder.AppendLine(title.faction.def.renounceTitleMessage);
		}
		WindowTool.Open(Dialog_MessageBox.CreateConfirmation("RenounceTitleDescription".Translate(pawn.Named("PAWN"), "TitleOfFaction".Translate(title.def.GetLabelCapFor(pawn), title.faction.GetCallLabel()).Named("TITLE"), stringBuilder.ToString().TrimEndNewlines().Named("EFFECTS")), delegate
		{
			pawn.royalty.SetTitle(title.faction, null, grantRewards: false);
			pawn.royalty.ResetPermitsAndPoints(title.faction, title.def);
		}, destructive: true));
		RoyalTitleDef FirstTitleWithPermit(RoyalTitlePermitDef permitDef)
		{
			return title.faction.def.RoyalTitlesAwardableInSeniorityOrderForReading.First((RoyalTitleDef t) => t.permits != null && t.permits.Contains(permitDef));
		}
	}

	internal static string GetPawnName(this Pawn pawn, bool needFull = false)
	{
		if (pawn == null || pawn.Name == null)
		{
			return "";
		}
		if (pawn.Name.GetType() == typeof(NameSingle))
		{
			return ((NameSingle)pawn.Name).Name;
		}
		if (pawn.Name.GetType() == typeof(NameTriple))
		{
			if (needFull)
			{
				return ((NameTriple)pawn.Name).ToStringFull;
			}
			return ((NameTriple)pawn.Name).ToStringShort;
		}
		return "";
	}

	internal static string GetPawnNameColored(this Pawn p, bool needFull = false)
	{
		if (p == null)
		{
			return "";
		}
		string pawnName = p.GetPawnName(needFull);
		string text = p.Name?.ToStringShort;
		text = text ?? "";
		if (!pawnName.NullOrEmpty())
		{
			if (pawnName.Contains("'" + text + "'"))
			{
				return pawnName.SubstringTo("'", withoutIt: false) + text.Colorize(ColorTool.colTan) + pawnName.SubstringBackwardFrom("'", withoutIt: false);
			}
			return pawnName.Replace(text, text.Colorize(ColorTool.colTan));
		}
		return pawnName ?? text;
	}

	internal static NameTriple GetPawnNameFromSeparatedString(string s)
	{
		if (!s.NullOrEmpty())
		{
			string[] array = s.Split(new string[1] { "?" }, StringSplitOptions.None);
			if (array.Length == 3)
			{
				string first = array[0];
				string nick = array[1];
				string last = array[2];
				return new NameTriple(first, nick, last);
			}
		}
		return PawnNameDatabaseSolid.GetListForGender(GenderPossibility.Either).RandomElement();
	}

	internal static NameSingle GetPawnNameFromSeparatedString(string s, PawnKindDef pkdFallback)
	{
		if (!s.NullOrEmpty())
		{
			return new NameSingle(s);
		}
		return new NameSingle((pkdFallback == null) ? "" : pkdFallback.label);
	}

	internal static string GetPawnNameAsSeparatedString(this Pawn pawn)
	{
		if (pawn == null || pawn.Name == null)
		{
			return "";
		}
		if (pawn.Name.GetType() == typeof(NameTriple))
		{
			NameTriple nameTriple = pawn.Name as NameTriple;
			return nameTriple.First + "?" + nameTriple.Nick + "?" + nameTriple.Last;
		}
		if (pawn.Name.GetType() == typeof(NameSingle))
		{
			NameSingle nameSingle = pawn.Name as NameSingle;
			return nameSingle.Name;
		}
		return "";
	}

	internal static void SetNameFromSeparatedString(this Pawn p, string s)
	{
		if (p == null || s.NullOrEmpty() || p.Name == null)
		{
			return;
		}
		try
		{
			if (p.Name.GetType() == typeof(NameSingle))
			{
				p.SetName(GetPawnNameFromSeparatedString(s, p.kindDef));
			}
			else
			{
				p.SetName(GetPawnNameFromSeparatedString(s));
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetName(this Pawn pawn, Name name)
	{
		if (pawn != null && name != null)
		{
			if (name.GetType() == typeof(NameTriple))
			{
				NameTriple nameTriple = name as NameTriple;
				pawn.Name = new NameTriple(nameTriple.First, nameTriple.Nick, nameTriple.Last);
			}
			else if (name.GetType() == typeof(NameSingle))
			{
				NameSingle nameSingle = name as NameSingle;
				pawn.Name = new NameSingle(nameSingle.Name, nameSingle.Numerical);
			}
		}
	}

	internal static void SetName(this Pawn pawn, string first, string nick, string last)
	{
		if (pawn != null)
		{
			pawn.Name = new NameTriple(first, nick, last);
		}
	}

	internal static Pawn GetPawnByNameTriple(NameTriple name)
	{
		foreach (Pawn item in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
		{
			if (item != null && item.Name != null && item.Name.GetType() == typeof(NameTriple) && item.Name.ToStringFull == name.ToStringFull)
			{
				return item;
			}
		}
		return null;
	}

	internal static Pawn GetPawnByNameSingle(NameSingle name)
	{
		if (name == null)
		{
			return null;
		}
		foreach (Pawn item in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
		{
			if (item != null && item.Name != null && item.Name.GetType() == typeof(NameSingle) && item.Name.ToStringFull == name.ToStringFull)
			{
				return item;
			}
		}
		return null;
	}
}
