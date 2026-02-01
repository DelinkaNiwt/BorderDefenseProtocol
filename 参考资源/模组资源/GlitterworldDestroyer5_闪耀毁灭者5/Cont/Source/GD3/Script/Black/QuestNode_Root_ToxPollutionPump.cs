using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestNode_Root_ToxPollutionPump : QuestNode
    {
		private const int DropPodsDelayTicks = 2500;

		private static readonly SimpleCurve WastepackCountOverPointsCurve = new SimpleCurve
	{
		new CurvePoint(200f, 90f),
		new CurvePoint(400f, 150f),
		new CurvePoint(800f, 225f),
		new CurvePoint(1600f, 325f),
		new CurvePoint(3200f, 400f),
		new CurvePoint(20000f, 1000f)
	};

		protected override bool TestRunInt(Slate slate)
		{
			if (QuestGen_Get.GetMap() != null)
			{
				return true;
			}
			return false;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Map map = QuestGen_Get.GetMap();
			slate.Set("map", map);
			float x = slate.Get("points", 0f);
			int wastepackCount = 5;
			quest.Delay(1200, delegate
			{
				List<Thing> list = new List<Thing>();
				for (int i = 0; i < wastepackCount; i++)
				{
					list.Add(ThingMaker.MakeThing(GDDefOf.Wastepack_Red));
				}
				quest.DropPods(map.Parent, list, "[wastepacksLetterLabel]", null, "[wastepacksLetterText]", null, true, useTradeDropSpot: false, joinPlayer: false, makePrisoners: false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, destroyItemsOnCleanup: true, dropAllInSamePod: true);
				QuestPart_Choice questPart_Choice = quest.RewardChoice();
				QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice
				{
					rewards =
					{
						(Reward)new Reward_BlackMech(),
					}
				};
				questPart_Choice.choices.Add(choice);
				quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			}, null, null, null, reactivatable: false, null, null, isQuestTimeout: false, null, null, null, tickHistorically: false, QuestPart.SignalListenMode.OngoingOnly, waitUntilPlayerHasHomeMap: true);
			slate.Set("wastepackCount", wastepackCount);
		}
	}
}
