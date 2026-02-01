using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using UnityEngine;

namespace GD3
{
	public class QuestNode_AdjustProgress : QuestNode
	{
		[NoTranslate]
		public SlateRef<string> inSignal;

		public SlateRef<int> progress;

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			int value = progress.GetValue(slate);
			QuestPart_AdjustProgress questPart_AdjustProgress = new QuestPart_AdjustProgress();
			questPart_AdjustProgress.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
			questPart_AdjustProgress.progress = value;
			QuestGen.quest.AddPart(questPart_AdjustProgress);
		}
	}
}