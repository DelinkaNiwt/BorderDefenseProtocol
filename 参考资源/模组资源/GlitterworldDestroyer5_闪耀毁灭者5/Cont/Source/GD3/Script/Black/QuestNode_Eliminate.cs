using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
	public class QuestNode_Eliminate : QuestNode
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
			string text = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
			string text2 = QuestGenUtility.HardcodedSignalWithQuestID("BattleStarted");
			string text3 = QuestGenUtility.HardcodedSignalWithQuestID("tesseron.Killed");
			string text4 = QuestGenUtility.HardcodedSignalWithQuestID("legionary.Killed");
			string text5 = QuestGenUtility.HardcodedSignalWithQuestID("winned");

			Pawn enemy1 = PawnGenerator.GeneratePawn(GDDefOf.Mech_BlackTesseron, Faction.OfMechanoids);
			if (!enemy1.IsWorldPawn())
            {
				Find.WorldPawns.PassToWorld(enemy1);
            }
			QuestGen.AddToGeneratedPawns(enemy1);
			Pawn enemy2 = PawnGenerator.GeneratePawn(GDDefOf.Mech_BlackLegionary, Faction.OfMechanoids);
			if (!enemy2.IsWorldPawn())
			{
				Find.WorldPawns.PassToWorld(enemy2);
			}
			QuestGen.AddToGeneratedPawns(enemy2);
			quest.ReservePawns(new List<Pawn> { enemy1, enemy2 });

			QuestGen.slate.Set("tesseron", enemy1);
			QuestGen.slate.Set("legionary", enemy2);

			quest.GenerateMilitor(site, text);
			quest.StartBattle(enemy1, site, enemy2, text2);
			quest.SignalPassAll(null, new List<string>(){text3, text4}, text5);
			quest.AddIntel(text5);
			quest.End(QuestEndOutcome.Success, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}
	}
}
