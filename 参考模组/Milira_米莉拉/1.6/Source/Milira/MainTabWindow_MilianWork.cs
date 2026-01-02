using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class MainTabWindow_MilianWork : MainTabWindow_PawnTable
{
	private const int SpaceBetweenPriorityArrowsAndWorkLabels = 40;

	protected override PawnTableDef PawnTableDef => MiliraDefOf.Milian_WorkSetting;

	protected override float ExtraTopSpace => 40f;

	protected override IEnumerable<Pawn> Pawns => from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
		where p.RaceProps.IsMechanoid && p.OverseerSubject != null && MilianUtility.IsMilian(p)
		select p;

	public override void DoWindowContents(Rect rect)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		base.DoWindowContents(rect);
		if ((int)Event.current.type != 8)
		{
			DoManualPrioritiesCheckbox();
			GUI.color = new Color(1f, 1f, 1f, 0.5f);
			Text.Anchor = TextAnchor.UpperCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(370f, rect.y + 5f, 160f, 30f), "<= " + "HigherPriority".Translate());
			Widgets.Label(new Rect(630f, rect.y + 5f, 160f, 30f), "LowerPriority".Translate() + " =>");
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect2 = new Rect(rect.x + 200f, rect.y, 32f, 32f);
			TooltipHandler.TipRegion(rect2, "Milira.ToMilianConfigTabTip".Translate());
			if (Widgets.ButtonImage(rect2, AncotLibraryIcon.Return))
			{
				Find.MainTabsRoot.SetCurrentTab(MiliraDefOf.Milian_Config);
			}
		}
	}

	private void DoManualPrioritiesCheckbox()
	{
		Text.Font = GameFont.Small;
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect = new Rect(5f, 5f, 140f, 30f);
		bool useWorkPriorities = Current.Game.playSettings.useWorkPriorities;
		Widgets.CheckboxLabeled(rect, "ManualPriorities".Translate(), ref Current.Game.playSettings.useWorkPriorities);
		if (useWorkPriorities != Current.Game.playSettings.useWorkPriorities)
		{
			foreach (Pawn item in PawnsFinder.AllMapsWorldAndTemporary_Alive)
			{
				if (item.Faction == Faction.OfPlayer && item.workSettings != null)
				{
					item.workSettings.Notify_UseWorkPrioritiesChanged();
				}
			}
		}
		if (Current.Game.playSettings.useWorkPriorities)
		{
			using (new TextBlock(new Color(1f, 1f, 1f, 0.5f)))
			{
				Widgets.Label(new Rect(rect.x, rect.yMax - 6f, rect.width, 60f), "PriorityOneDoneFirst".Translate());
			}
		}
		if (!Current.Game.playSettings.useWorkPriorities)
		{
			UIHighlighter.HighlightOpportunity(rect, "ManualPriorities-Off");
		}
	}
}
