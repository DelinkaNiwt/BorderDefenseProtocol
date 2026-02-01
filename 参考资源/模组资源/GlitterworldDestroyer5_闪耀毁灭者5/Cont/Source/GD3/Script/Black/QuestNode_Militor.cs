using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_Militor : QuestNode
	{
		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Pawn militor = quest.GeneratePawn(new PawnGenerationRequest(GDDefOf.Mech_Militor, GDUtility.BlackMechanoid, PawnGenerationContext.NonPlayer, null, true));
			quest.ReservePawns(Gen.YieldSingle(militor));
			QuestGen.slate.Set("militor", militor);
			Site site = slate.Get<Site>("site");
			string mapGenerated = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");

			QuestPart_PlaceMilitor questPart_PlaceMilitor = new QuestPart_PlaceMilitor();
			questPart_PlaceMilitor.militor = militor;
			questPart_PlaceMilitor.site = site;
			questPart_PlaceMilitor.inSignal = mapGenerated;
			quest.AddPart(questPart_PlaceMilitor);

			string text = QuestGenUtility.HardcodedSignalWithQuestID("militor.raidLight");
			string text2 = QuestGenUtility.HardcodedSignalWithQuestID("militor.raidHeavy");
			string text3 = QuestGenUtility.HardcodedSignalWithQuestID("militor.Killed");
			string text4 = QuestGenUtility.HardcodedSignalWithQuestID("militor.raidUltraHeavy");
			string text5 = QuestGenUtility.HardcodedSignalWithQuestID("militor.helpArrive");
			string text6 = QuestGenUtility.HardcodedSignalWithQuestID("militor.LeftMap");
			quest.InitiateRaid(militor, 4000f, "GD.RaidLight", "GD.RaidLightDesc", text);
			quest.InitiateRaid(militor, 8000f, "GD.RaidHeavy", "GD.RaidHeavyDesc", text2);
			quest.InitiateRaid(militor, 15000f, "GD.RaidUltraHeavy", "GD.RaidUltraHeavyDesc", text4);

            quest.SignalPass(delegate
            {
				quest.InitiateComet(militor, GDDefOf.Mech_BlackLancer, GDDefOf.BlackMechanoid);
				quest.InitiateComet(militor, GDDefOf.Mech_BlackScyther, GDDefOf.BlackMechanoid);
			}, text5);

			quest.AddIntel(text6);
			quest.End(QuestEndOutcome.Fail, 0, null, text3, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
			quest.End(QuestEndOutcome.Success, 0, null, text6, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}
	}
}
