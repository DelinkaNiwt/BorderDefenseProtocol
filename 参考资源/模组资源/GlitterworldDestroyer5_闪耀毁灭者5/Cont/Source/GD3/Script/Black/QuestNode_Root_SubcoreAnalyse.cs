using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_Root_SubcoreAnalyze : QuestNode
	{

		protected override bool TestRunInt(Slate slate)
		{
			if (QuestGen_Get.GetMap() != null)
			{
				return true;
			}
			return false;
			//QuestNode_Root_ShuttleCrash_Rescue
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			QuestPart_Choice questPart_Choice = quest.RewardChoice();
			QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice
			{
				rewards =
					{
						(Reward)new Reward_BlackMech(),
					}
			};
			questPart_Choice.choices.Add(choice);
			Map map = QuestGen_Get.GetMap();
			slate.Set("map", map);
			float x = slate.Get("points", 0f);
			Thing subcore = ThingMaker.MakeThing(GDDefOf.GD_Subcore);
			slate.Set("faction", Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid));
			slate.Set("subcore", subcore);
			quest.Delay(1200, delegate
			{
				List<Thing> list = new List<Thing>();
				list.Add(subcore);
				quest.DropPods(map.Parent, list, "[subcoreArriveLetterLabel]", null, "[subcoreArriveLetterText]", null, true, useTradeDropSpot: true, joinPlayer: false, makePrisoners: false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, destroyItemsOnCleanup: true, dropAllInSamePod: true);
				
			});
			string text = QuestGenUtility.HardcodedSignalWithQuestID("subcore.Analyzed");
			string questSuccess = QuestGenUtility.HardcodedSignalWithQuestID("questSuccess");
			string text2 = QuestGenUtility.HardcodedSignalWithQuestID("subcore.Destroyed");
			string text3 = QuestGenUtility.HardcodedSignalWithQuestID("faction.BecameHostileToPlayer");
			quest.SignalPass(null, text, questSuccess);
			quest.PlaySubcoreEffect(subcore, EffecterDefOf.Skip_Entry, map, questSuccess);
			quest.End(QuestEndOutcome.Fail, 0, null, text2, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
			quest.End(QuestEndOutcome.Fail, 0, null, text3, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}
	}
}
