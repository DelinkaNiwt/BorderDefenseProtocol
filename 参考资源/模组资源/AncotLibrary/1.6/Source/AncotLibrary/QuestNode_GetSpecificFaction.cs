using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_GetSpecificFaction : QuestNode
{
	public SlateRef<FactionDef> factionDef;

	[NoTranslate]
	public SlateRef<string> storeFactionAs;

	[NoTranslate]
	public SlateRef<string> storeFactionLeaderAs;

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef.GetValue(slate));
		slate.Set(storeFactionAs.GetValue(slate), faction);
		if (!storeFactionLeaderAs.GetValue(slate).NullOrEmpty())
		{
			QuestGen.slate.Set(storeFactionLeaderAs.GetValue(slate), faction.leader);
		}
		if (faction != null && !faction.Hidden)
		{
			QuestPart_InvolvedFactions questPart_InvolvedFactions = new QuestPart_InvolvedFactions();
			questPart_InvolvedFactions.factions.Add(faction);
			QuestGen.quest.AddPart(questPart_InvolvedFactions);
		}
	}

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}
}
