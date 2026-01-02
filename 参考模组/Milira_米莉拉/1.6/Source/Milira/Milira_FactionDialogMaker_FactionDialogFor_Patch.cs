using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
public static class Milira_FactionDialogMaker_FactionDialogFor_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ref DiaNode __result, Pawn negotiator, Faction faction)
	{
		Map map = negotiator.Map;
		if (__result == null || !(faction.def.defName == "Milira_AngelismChurch"))
		{
			return;
		}
		RoyalTitleDef royalTitleDef = negotiator.royalty?.GetCurrentTitle(faction).GetNextTitle(faction);
		if (royalTitleDef != null)
		{
			DiaOption diaOption = TitleDiaOption(map, faction, negotiator);
			__result.options.Insert(0, diaOption);
			if (!negotiator.royalty.CanUpdateTitle(faction))
			{
				diaOption.Disable("Milira.TitleUpdate_Disabled".Translate());
			}
		}
	}

	public static DiaOption TitleDiaOption(Map map, Faction faction, Pawn negotiator)
	{
		RoyalTitleDef nextTitle = negotiator.royalty?.GetCurrentTitle(faction).GetNextTitle(faction);
		DiaOption diaOption = new DiaOption("Milira.TitleUpdate".Translate(negotiator));
		diaOption.action = delegate
		{
			negotiator.royalty.TryUpdateTitle(faction, sendLetter: true, nextTitle);
		};
		DiaNode diaNode = (diaOption.link = new DiaNode("Milira.TitleUpdate_Success".Translate(negotiator, faction, nextTitle, "\n-" + string.Join("\n-", nextTitle.permits.Select((RoyalTitlePermitDef p) => p.label.CapitalizeFirst())), negotiator.royalty.GetFavor(faction))));
		DiaOption diaOption2 = new DiaOption("Ancot.Finish".Translate());
		diaOption2.linkLateBind = () => FactionDialogMaker.FactionDialogFor(negotiator, faction);
		diaNode.options.Add(diaOption2);
		return diaOption;
	}
}
