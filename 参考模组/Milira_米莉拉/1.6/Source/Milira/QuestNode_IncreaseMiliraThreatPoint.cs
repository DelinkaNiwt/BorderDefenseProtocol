using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_IncreaseMiliraThreatPoint : QuestNode
{
	public SlateRef<int> miliraThreatPoint = 0;

	[NoTranslate]
	public SlateRef<string> inSignal;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		if (miliraThreatPoint != 0)
		{
			QuestPart_IncreaseMiliraThreatPoint questPart_IncreaseMiliraThreatPoint = new QuestPart_IncreaseMiliraThreatPoint();
			questPart_IncreaseMiliraThreatPoint.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
			questPart_IncreaseMiliraThreatPoint.miliraThreatPoint = miliraThreatPoint.GetValue(slate);
			QuestGen.quest.AddPart(questPart_IncreaseMiliraThreatPoint);
		}
	}
}
