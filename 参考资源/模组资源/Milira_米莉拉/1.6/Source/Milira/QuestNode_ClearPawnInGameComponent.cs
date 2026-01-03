using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_ClearPawnInGameComponent : QuestNode
{
	[NoTranslate]
	public SlateRef<string> inSignal;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		QuestPart_ClearPawnInGameComponent questPart_ClearPawnInGameComponent = new QuestPart_ClearPawnInGameComponent();
		questPart_ClearPawnInGameComponent.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
		QuestGen.quest.AddPart(questPart_ClearPawnInGameComponent);
	}
}
