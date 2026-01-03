using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class MainTabWindow_MilianConfig : MainTabWindow_PawnTable
{
	protected override PawnTableDef PawnTableDef => MiliraDefOf.Milian_ConfigTable;

	protected override float ExtraTopSpace => 35f;

	protected override IEnumerable<Pawn> Pawns => from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
		where p.RaceProps.IsMechanoid && p.OverseerSubject != null && MilianUtility.IsMilian(p)
		select p;

	public override void DoWindowContents(Rect rect)
	{
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Invalid comparison between Unknown and I4
		if (!ModLister.BiotechInstalled)
		{
			return;
		}
		base.DoWindowContents(rect);
		Rect rect2 = new Rect(rect.x, rect.y, 240f, 32f);
		Text.Font = GameFont.Small;
		if (Widgets.ButtonText(rect2, "Milira.MilianConfigGuide".Translate()))
		{
			Find.WindowStack.Add(new Dialog_NodeTree(MilianConfigGuide()));
		}
		Rect rect3 = new Rect(rect.x + 240f, rect.y, 32f, 32f);
		TooltipHandler.TipRegion(rect3, "Milira.ToMechTabTip".Translate());
		if (Widgets.ButtonImage(rect3, AncotLibraryIcon.Return))
		{
			Find.MainTabsRoot.SetCurrentTab(MiliraDefOf.Mechs);
		}
		Rect rect4 = new Rect(rect.x + 280f, rect.y, 32f, 32f);
		if (MiliraDefOf.Milira_MilianTech_WorkManagement.IsFinished && (int)MiliraRaceSettings.TabAvailable_MilianWork != 1)
		{
			TooltipHandler.TipRegion(rect4, "Milira.ToMilianWorkManagementTabTip".Translate());
			if (Widgets.ButtonImage(rect4, AncotLibraryIcon.Hammer))
			{
				Find.MainTabsRoot.SetCurrentTab(MiliraDefOf.Milian_Work);
			}
		}
	}

	public static DiaNode MilianConfigGuide()
	{
		string text = "Milira.MilianConfigGuideDesc".Translate();
		for (int i = 0; i < 4; i++)
		{
			string key = "Milira.MilianConfigGuidePage" + i;
			text += "\n\n" + key.Translate();
		}
		DiaNode diaNode = new DiaNode(text);
		DiaOption item = new DiaOption("Ancot.Finish".Translate())
		{
			resolveTree = true
		};
		diaNode.options.Add(item);
		return diaNode;
	}
}
