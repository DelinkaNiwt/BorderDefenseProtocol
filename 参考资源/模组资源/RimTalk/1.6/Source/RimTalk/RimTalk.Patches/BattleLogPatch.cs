using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Source.Data;
using RimTalk.Util;
using Verse;

namespace RimTalk.Patches;

[HarmonyPatch(typeof(BattleLog), "Add")]
public static class BattleLogPatch
{
	private static void Postfix(LogEntry entry)
	{
		List<Pawn> pawnsInvolved = entry.GetConcerns().OfType<Pawn>().ToList();
		if (pawnsInvolved.Count < 2)
		{
			return;
		}
		Pawn initiator = pawnsInvolved[0];
		Pawn recipient = pawnsInvolved[1];
		if (Cache.Get(initiator) == null && Cache.Get(recipient) == null)
		{
			return;
		}
		string prompt = GenerateDirectPrompt(entry, initiator, recipient);
		if (string.IsNullOrEmpty(prompt))
		{
			return;
		}
		Cache.Get(initiator)?.AddTalkRequest(prompt, recipient, TalkType.Urgent);
		Cache.Get(recipient)?.AddTalkRequest(prompt, initiator, TalkType.Urgent);
		List<Pawn> pawns = PawnSelector.GetNearByTalkablePawns(initiator, recipient, PawnSelector.DetectionType.Viewing);
		foreach (Pawn pawn in pawns.Take(2))
		{
			Cache.Get(pawn)?.AddTalkRequest(prompt, initiator, TalkType.Urgent);
		}
	}

	private static string GenerateDirectPrompt(LogEntry entry, Pawn initiator, Pawn recipient)
	{
		try
		{
			string initiatorLabel = initiator.LabelShort + "(" + initiator.GetRole() + ")";
			string recipientLabel = recipient.LabelShort + "(" + recipient.GetRole() + ")";
			if (entry is BattleLogEntry_RangedImpact impactEntry)
			{
				Traverse traverse = Traverse.Create(impactEntry);
				ThingDef weaponDef = traverse.Field<ThingDef>("weaponDef").Value;
				ThingDef projectileDef = traverse.Field<ThingDef>("projectileDef").Value;
				string weaponLabel = weaponDef?.label ?? projectileDef?.label ?? "a projectile";
				bool deflected = traverse.Field<bool>("deflected").Value;
				List<BodyPartRecord> damagedParts = traverse.Field<List<BodyPartRecord>>("damagedParts").Value;
				if (deflected)
				{
					return initiatorLabel + "'s shot with " + weaponLabel + " at " + recipientLabel + " was deflected.";
				}
				if (damagedParts == null || damagedParts.Count == 0)
				{
					return initiatorLabel + " missed " + recipientLabel + " with " + weaponLabel + ".";
				}
				return initiatorLabel + " hit " + recipientLabel + " with " + weaponLabel + ".";
			}
			if (entry is BattleLogEntry_MeleeCombat meleeEntry)
			{
				Traverse traverse2 = Traverse.Create(meleeEntry);
				RulePackDef ruleDef = traverse2.Field<RulePackDef>("ruleDef").Value;
				if (ruleDef == null)
				{
					return null;
				}
				string ruleDefName = ruleDef.defName;
				string toolLabel = traverse2.Field<string>("toolLabel").Value;
				if (ruleDefName == "Combat_MeleeBite")
				{
					return initiatorLabel + " bit " + recipientLabel + ".";
				}
				if (ruleDefName == "Combat_MeleeScratch")
				{
					return initiatorLabel + " scratched " + recipientLabel + ".";
				}
				if (!string.IsNullOrEmpty(toolLabel))
				{
					return initiatorLabel + " hit " + recipientLabel + " with their " + toolLabel + ".";
				}
				return initiatorLabel + " attacked " + recipientLabel + " in melee.";
			}
		}
		catch (Exception ex)
		{
			Logger.ErrorOnce("Battle prompt generation failed.\n " + ex.Message, entry.GetHashCode());
			return entry.ToGameStringFromPOV(initiator).StripTags();
		}
		return null;
	}
}
