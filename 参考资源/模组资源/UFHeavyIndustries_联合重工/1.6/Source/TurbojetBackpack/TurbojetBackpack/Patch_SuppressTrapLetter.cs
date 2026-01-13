using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(LetterStack), "ReceiveLetter", new Type[]
{
	typeof(TaggedString),
	typeof(TaggedString),
	typeof(LetterDef),
	typeof(LookTargets),
	typeof(Faction),
	typeof(Quest),
	typeof(List<ThingDef>),
	typeof(string),
	typeof(int),
	typeof(bool)
})]
public static class Patch_SuppressTrapLetter
{
	public static bool Prefix(TaggedString label, LetterDef textLetterDef, LookTargets lookTargets)
	{
		if (textLetterDef != LetterDefOf.NegativeEvent && textLetterDef != LetterDefOf.ThreatSmall)
		{
			return true;
		}
		if (lookTargets == null || !lookTargets.PrimaryTarget.IsValid)
		{
			return true;
		}
		Map map = lookTargets.PrimaryTarget.Map;
		IntVec3 cell = lookTargets.PrimaryTarget.Cell;
		if (map == null || !cell.IsValid)
		{
			return true;
		}
		List<Thing> thingList = cell.GetThingList(map);
		Pawn pawn = null;
		for (int i = 0; i < thingList.Count; i++)
		{
			if (thingList[i] is Pawn pawn2 && TurbojetGlobal.IsFlightActive(pawn2))
			{
				pawn = pawn2;
				break;
			}
		}
		if (pawn == null)
		{
			return true;
		}
		string text = label.ToString().StripTags().Trim();
		string text2 = pawn.LabelShort.StripTags();
		string text3 = "LetterFriendlyTrapSprungLabel".Translate(text2, pawn).ToString().StripTags()
			.Trim();
		if (text == text3)
		{
			return false;
		}
		return true;
	}
}
