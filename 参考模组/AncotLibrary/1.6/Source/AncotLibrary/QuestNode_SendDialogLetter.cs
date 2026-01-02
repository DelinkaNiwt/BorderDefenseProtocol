using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_SendDialogLetter : QuestNode
{
	public SlateRef<TaggedString> label;

	public SlateRef<TaggedString> text;

	public SlateRef<LetterDef> letterDef;

	public SlateRef<Faction> faction;

	public SlateRef<int> limitTicks = -1;

	[NoTranslate]
	public SlateRef<string> inSignal;

	[NoTranslate]
	public SlateRef<string> outSignal;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		QuestPart_SendDialogLetter questPart_SendDialogLetter = new QuestPart_SendDialogLetter();
		questPart_SendDialogLetter.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal");
		questPart_SendDialogLetter.outSignal = QuestGenUtility.HardcodedSignalWithQuestID(outSignal.GetValue(slate)) ?? null;
		questPart_SendDialogLetter.faction = faction.GetValue(slate);
		questPart_SendDialogLetter.points = slate.Get("points", 0f);
		questPart_SendDialogLetter.label = label.GetValue(slate);
		questPart_SendDialogLetter.text = text.GetValue(slate);
		questPart_SendDialogLetter.letterDef = letterDef.GetValue(slate);
		questPart_SendDialogLetter.limitTicks = limitTicks.GetValue(slate);
		QuestGen.quest.AddPart(questPart_SendDialogLetter);
	}
}
