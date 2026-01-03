using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_GetSpecificPawnInMap : QuestNode
{
	[NoTranslate]
	public SlateRef<string> inSignal;

	[NoTranslate]
	public SlateRef<Pawn> specificPawn;

	public QuestNode node;

	public QuestNode elseNode;

	protected override bool TestRunInt(Slate slate)
	{
		if (node != null)
		{
			return node.TestRun(slate);
		}
		if (elseNode != null)
		{
			return elseNode.TestRun(slate);
		}
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Map map = QuestGen_Get.GetMap();
		QuestPart_GetSpecificPawnInMap questPart_GetSpecificPawnInMap = new QuestPart_GetSpecificPawnInMap();
		questPart_GetSpecificPawnInMap.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
		questPart_GetSpecificPawnInMap.map = map;
		questPart_GetSpecificPawnInMap.specificPawn = specificPawn;
		questPart_GetSpecificPawnInMap.node = node;
		questPart_GetSpecificPawnInMap.elseNode = elseNode;
		QuestGen.quest.AddPart(questPart_GetSpecificPawnInMap);
	}
}
