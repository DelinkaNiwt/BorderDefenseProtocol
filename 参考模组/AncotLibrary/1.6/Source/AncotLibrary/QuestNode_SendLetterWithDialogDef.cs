using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_SendLetterWithDialogDef : QuestNode
{
	public SlateRef<TaggedString> label;

	public SlateRef<TaggedString> text;

	public SlateRef<LetterDef> letterDef;

	public SlateRef<DialogDef> dialogDef;

	public SlateRef<Faction> faction;

	public SlateRef<int> limitTicks = -1;

	[NoTranslate]
	public SlateRef<string> inSignal;

	[NoTranslate]
	public SlateRef<string> outSignal;

	[NoTranslate]
	public SlateRef<string> optionSignalA;

	[NoTranslate]
	public SlateRef<string> optionSignalB;

	[NoTranslate]
	public SlateRef<string> optionSignalC;

	[NoTranslate]
	public SlateRef<string> optionSignalD;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Quest quest = QuestGen.quest;
		QuestPart_SendLetterWithDialogDef questPart_SendLetterWithDialogDef = new QuestPart_SendLetterWithDialogDef();
		questPart_SendLetterWithDialogDef.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal");
		questPart_SendLetterWithDialogDef.outSignal = QuestGenUtility.HardcodedSignalWithQuestID(outSignal.GetValue(slate)) ?? null;
		questPart_SendLetterWithDialogDef.quest = quest;
		questPart_SendLetterWithDialogDef.faction = faction.GetValue(slate);
		questPart_SendLetterWithDialogDef.points = slate.Get("points", 0f);
		questPart_SendLetterWithDialogDef.label = label.GetValue(slate);
		questPart_SendLetterWithDialogDef.text = text.GetValue(slate);
		questPart_SendLetterWithDialogDef.letterDef = letterDef.GetValue(slate);
		questPart_SendLetterWithDialogDef.dialogDef = dialogDef.GetValue(slate);
		questPart_SendLetterWithDialogDef.limitTicks = limitTicks.GetValue(slate);
		questPart_SendLetterWithDialogDef.optionSignalA = QuestGenUtility.HardcodedSignalWithQuestID(optionSignalA.GetValue(slate)) ?? null;
		questPart_SendLetterWithDialogDef.optionSignalB = QuestGenUtility.HardcodedSignalWithQuestID(optionSignalB.GetValue(slate)) ?? null;
		questPart_SendLetterWithDialogDef.optionSignalC = QuestGenUtility.HardcodedSignalWithQuestID(optionSignalC.GetValue(slate)) ?? null;
		questPart_SendLetterWithDialogDef.optionSignalD = QuestGenUtility.HardcodedSignalWithQuestID(optionSignalD.GetValue(slate)) ?? null;
		QuestGen.quest.AddPart(questPart_SendLetterWithDialogDef);
	}
}
