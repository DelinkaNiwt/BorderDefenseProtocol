using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace GD3
{
	public class QuestNode_Root_SavingMechModified : QuestNode
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
			int num2 = 30000;
			slate.Set("rewardDelayTicks", num);
			quest.Delay(num, delegate
			{
				ThingSetMakerParams parms = default(ThingSetMakerParams);
				parms.totalMarketValueRange = marketValueRange;
				parms.qualityGenerator = QualityGenerator.Reward;
				List<Thing> list = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
				slate.Set("listOfRewards", GenLabel.ThingsLabel(list));
				quest.DropPods(map.Parent, list, null, null, null, null, false, useTradeDropSpot: true);
				string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("Rejected");
				string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("Ended");
				string inSignal3 = QuestGenUtility.HardcodedSignalWithQuestID("Letter");

				quest.SignalPass(null, null, inSignal3);
				QuestPart_ReceiveLetter questPart_ReceiveLetter = new QuestPart_ReceiveLetter();
				questPart_ReceiveLetter.signalListenMode = QuestPart.SignalListenMode.Always;
				questPart_ReceiveLetter.inSignal = inSignal3;
				questPart_ReceiveLetter.signalRejected = inSignal;
				questPart_ReceiveLetter.outSignal = inSignal2;
				QuestGen.AddTextRequest("root", delegate (string x)
				{
					questPart_ReceiveLetter.title = x;
				}, QuestGenUtility.MergeRules(null, "[rewardLetterLabel]", "root"));
				QuestGen.AddTextRequest("root", delegate (string x)
				{
					questPart_ReceiveLetter.letterText = x;
				}, QuestGenUtility.MergeRules(null, "[rewardLetterText]", "root"));
				QuestGen.AddTextRequest("root", delegate (string x)
				{
					questPart_ReceiveLetter.subLetterText = x;
				}, QuestGenUtility.MergeRules(null, "[subLetterText]", "root"));
				quest.AddPart(questPart_ReceiveLetter);

				quest.Signal(inSignal, delegate
				{
					QuestPart_ReceiveLetter questPart_ReceiveLetter2 = new QuestPart_ReceiveLetter();
					questPart_ReceiveLetter2.signalListenMode = QuestPart.SignalListenMode.Always;
					questPart_ReceiveLetter2.inSignal = inSignal;
					QuestGen.AddTextRequest("root", delegate (string x)
					{
						questPart_ReceiveLetter2.title = x;
					}, QuestGenUtility.MergeRules(null, "[endingLetterLabel]", "root"));
					QuestGen.AddTextRequest("root", delegate (string x)
					{
						questPart_ReceiveLetter2.letterText = x;
					}, QuestGenUtility.MergeRules(null, "[endingLetterText]", "root"));
					questPart_ReceiveLetter2.delayTick = num2;
					quest.AddPart(questPart_ReceiveLetter2);
					QuestPart_AdjustProgress questPart_AdjustProgress = new QuestPart_AdjustProgress();
					questPart_AdjustProgress.inSignal = inSignal;
					questPart_AdjustProgress.progress = 3;
					QuestGen.quest.AddPart(questPart_AdjustProgress);
				}, null, QuestPart.SignalListenMode.Always);

				quest.End(QuestEndOutcome.Unknown, 0, null, inSignal2);
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
