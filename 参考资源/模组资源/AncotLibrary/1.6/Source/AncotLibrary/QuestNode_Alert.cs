using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_Alert : QuestNode
{
	public SlateRef<string> label;

	public SlateRef<string> explanation;

	[NoTranslate]
	public SlateRef<string> insignalEnable;

	[NoTranslate]
	public SlateRef<string> insignalDisable;

	public SlateRef<bool> getLookTargetsFromSignal = false;

	public SlateRef<LookTargets> lookTargets;

	public SlateRef<bool> critical = false;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Quest quest = QuestGen.quest;
		quest.Alert(label.GetValue(slate), explanation.GetValue(slate), lookTargets.GetValue(slate), critical.GetValue(slate), getLookTargetsFromSignal.GetValue(slate), insignalEnable.GetValue(slate), insignalDisable.GetValue(slate));
	}
}
