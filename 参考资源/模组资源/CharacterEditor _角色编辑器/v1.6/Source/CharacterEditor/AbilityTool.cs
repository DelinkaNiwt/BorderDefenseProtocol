using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class AbilityTool
{
	internal const string CO_CURRENTENTROPY = "currentEntropy";

	internal const string CO_CURRENTPSYFOCUS = "currentPsyfocus";

	internal static List<Ability> GetCopyOfAbilites(this Pawn p)
	{
		if (!p.HasAbilityTracker() || p.abilities.abilities.NullOrEmpty())
		{
			return new List<Ability>();
		}
		Ability[] array = new Ability[p.abilities.abilities.Count];
		p.abilities.abilities.CopyTo(array);
		return array.ToList();
	}

	internal static void RemoveTemporaryAbilities(this Pawn p, List<Ability> copyOriginalAbilities)
	{
		if (!p.HasAbilityTracker())
		{
			return;
		}
		for (int num = p.abilities.abilities.Count - 1; num >= 0; num--)
		{
			if (copyOriginalAbilities.NullOrEmpty() || !copyOriginalAbilities.Contains(p.abilities.abilities[num]))
			{
				p.abilities.abilities.Remove(p.abilities.abilities[num]);
			}
		}
	}

	internal static void CheckAddPsylink(this Pawn p, int level = -1)
	{
		if (!ModsConfig.RoyaltyActive || !p.HasPsyTracker())
		{
			return;
		}
		Hediff_Psylink mainPsylinkSource = p.GetMainPsylinkSource();
		bool flag = mainPsylinkSource == null;
		if (mainPsylinkSource == null)
		{
			List<Ability> copyOfAbilites = p.GetCopyOfAbilites();
			p.ChangePsylinkLevel((level <= 0) ? 1 : level);
			p.RemoveTemporaryAbilities(copyOfAbilites);
			p.psychicEntropy.Notify_GainedPsylink();
			p.psychicEntropy.PsychicEntropyTrackerTickInterval(0);
		}
		else if (level >= 0)
		{
			p.ChangePsylinkLevel(level - mainPsylinkSource.level);
		}
		mainPsylinkSource = p.GetMainPsylinkSource();
		if (mainPsylinkSource != null)
		{
			if (level > 0 && mainPsylinkSource.level != level)
			{
				mainPsylinkSource.level = level;
				mainPsylinkSource.Severity = level;
			}
			p.health.Notify_HediffChanged(mainPsylinkSource);
		}
	}

	internal static float GetPsyfocus(this Pawn p)
	{
		return p.HasPsylink ? p.psychicEntropy.CurrentPsyfocus : 0f;
	}

	internal static float GetEntropy(this Pawn p)
	{
		return p.HasPsylink ? p.psychicEntropy.EntropyValue : 0f;
	}

	internal static void SetEntropy(this Pawn p, float val)
	{
		if (p.HasPsylink)
		{
			p.psychicEntropy.SetMemberValue("currentEntropy", val);
		}
	}

	internal static void SetPsyfocus(this Pawn p, float val)
	{
		if (!p.HasPsylink)
		{
			return;
		}
		try
		{
			p.psychicEntropy.SetMemberValue("currentPsyfocus", val);
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static string GetPsyAbilitiesAsSeparatedString(this Pawn p)
	{
		if (!p.HasAbilityTracker())
		{
			return "";
		}
		string text = "";
		foreach (Ability ability in p.abilities.abilities)
		{
			text = text + ability.def.defName + ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetPsyAbilitiesFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasAbilityTracker())
		{
			return;
		}
		try
		{
			string[] array = s.Split(new string[1] { ":" }, StringSplitOptions.None);
			string[] array2 = array;
			foreach (string defName in array2)
			{
				AbilityDef abilityDef = DefTool.AbilityDef(defName);
				if (abilityDef != null)
				{
					p.abilities.GainAbility(abilityDef);
				}
			}
			p.abilities.Notify_TemporaryAbilitiesChanged();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}
}
