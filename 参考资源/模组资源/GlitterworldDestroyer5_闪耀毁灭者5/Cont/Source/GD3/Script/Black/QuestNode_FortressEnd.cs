using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
	public class QuestNode_FortressEnd : QuestNode
	{
		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Site site = slate.Get<Site>("site");
			string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
			string text = QuestGenUtility.HardcodedSignalWithQuestID("serverStolen");
			QuestPart_PlaceThing questPart_PlaceThing = new QuestPart_PlaceThing();
			questPart_PlaceThing.inSignal = inSignal;
			questPart_PlaceThing.thingDef = GDDefOf.GD_ServerDummy;
			questPart_PlaceThing.site = site;
			quest.AddPart(questPart_PlaceThing);
			quest.AddIntel(text);
			quest.End(QuestEndOutcome.Success, 0, null, text, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}
	}
}
