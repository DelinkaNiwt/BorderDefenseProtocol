using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_AddThingToList : QuestNode
{
	[NoTranslate]
	public SlateRef<string> name;

	public SlateRef<Thing> thing;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		QuestGenUtility.AddToOrMakeList(QuestGen.slate, name.GetValue(slate), thing.GetValue(slate));
	}
}
