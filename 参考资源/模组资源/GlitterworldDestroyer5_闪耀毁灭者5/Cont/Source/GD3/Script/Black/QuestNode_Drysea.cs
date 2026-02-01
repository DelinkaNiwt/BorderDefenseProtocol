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
	public class QuestNode_Drysea : QuestNode
	{
		public SlateRef<Site> site;

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Site site = this.site.GetValue(slate);
			QuestGen.slate.Set("site", site);
			Pawn pawn = PawnGenerator.GeneratePawn(GetApocriton(), Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid));
			QuestGen.AddToGeneratedPawns(pawn);
			if (!pawn.IsWorldPawn())
            {
				Find.WorldPawns.PassToWorld(pawn);
            }
			quest.ReservePawns(Gen.YieldSingle(pawn));
			QuestGen.slate.Set("pawn", pawn);

			string text = QuestGenUtility.HardcodedSignalWithQuestID("Teleported");
			string text2 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.CommDie");
			string text3 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.CommLive");
			string text4 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Complete");
			string text5 = QuestGenUtility.HardcodedSignalWithQuestID("site.Destroyed");
			string text6 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");

			QuestPart_PlaceThing questPart_PlaceThing = new QuestPart_PlaceThing();
			questPart_PlaceThing.inSignal = text6;
			questPart_PlaceThing.thingDef = GDDefOf.GD_DryseaDummy;
			questPart_PlaceThing.site = site;
			quest.AddPart(questPart_PlaceThing);
			quest.GenerateDrysea(site, pawn, text);
			quest.InitiateScript(pawn.Map, pawn, GetScript("Scripts_Apocriton_true"), text2);
			quest.InitiateScript(pawn.Map, pawn, GetScript("Scripts_Apocriton_false"), text3);
			quest.CheckQuestFail(text5);

			quest.BackHome(pawn, text4);
			quest.AddIntel(text4);
			quest.End(QuestEndOutcome.Success, 0, null, text4, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}

		private PawnKindDef GetApocriton()
        {
			return DefDatabase<PawnKindDef>.AllDefs.First(p => p.defName == "Mech_BlackApocriton");
        }

		private MechanoidScriptDef GetScript(string str)
		{
			return DefDatabase<MechanoidScriptDef>.AllDefs.First(p => p.defName == str);
		}
	}
}
