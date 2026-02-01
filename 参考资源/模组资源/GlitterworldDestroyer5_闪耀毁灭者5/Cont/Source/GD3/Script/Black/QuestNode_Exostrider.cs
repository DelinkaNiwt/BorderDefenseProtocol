using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
	public class QuestNode_Exostrider : QuestNode
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
			string text = QuestGenUtility.HardcodedSignalWithQuestID("exostriderDestroyed");
			
			QuestPart_PlaceThing questPart_PlaceThing = new QuestPart_PlaceThing();
			questPart_PlaceThing.inSignal = inSignal;
			questPart_PlaceThing.thingDef = GDDefOf.GD_ExostriderDummy;
			questPart_PlaceThing.site = site;
			quest.AddPart(questPart_PlaceThing);
			quest.End(QuestEndOutcome.Success, 0, null, text, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}
	}
}
