using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace GD3
{
	public class QuestNode_Root_SavingMechNotModified : QuestNode
	{
		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Map map = QuestGen_Get.GetMap();
			FloatRange marketValueRange = slate.Get<FloatRange>("marketValueRange");
			Pawn val = slate.Get<Pawn>("rewardGiver");
			quest.ReservePawns(Gen.YieldSingle(val));
			int num = Rand.Range(5, 20) * 60000;
			slate.Set("rewardDelayTicks", num);
			quest.Delay(num, delegate
			{
				ThingSetMakerParams parms = default(ThingSetMakerParams);
				parms.totalMarketValueRange = marketValueRange;
				parms.qualityGenerator = QualityGenerator.Reward;
				List<Thing> list = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
				slate.Set("listOfRewards", GenLabel.ThingsLabel(list));
				quest.DropPods(map.Parent, list, null, null, "[rewardLetterText]", null, true, useTradeDropSpot: true);
				QuestPart_AdjustProgress questPart_AdjustProgress = new QuestPart_AdjustProgress();
				questPart_AdjustProgress.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(QuestGen.slate.Get<string>("inSignal"));
				questPart_AdjustProgress.progress = 1;
				QuestGen.quest.AddPart(questPart_AdjustProgress);
				QuestGen_End.End(quest, QuestEndOutcome.Unknown);
			}, null, null, null, reactivatable: false, null, null, isQuestTimeout: false, null, null, "RewardDelay", tickHistorically: false, QuestPart.SignalListenMode.OngoingOnly, waitUntilPlayerHasHomeMap: true);
		}

		protected override bool TestRunInt(Slate slate)
		{
			if (slate.Get<Pawn>("rewardGiver") != null && slate.TryGet<FloatRange>("marketValueRange", out var _))
			{
				return QuestGen_Get.GetMap() != null;
			}
			return false;
		}
	}

}
