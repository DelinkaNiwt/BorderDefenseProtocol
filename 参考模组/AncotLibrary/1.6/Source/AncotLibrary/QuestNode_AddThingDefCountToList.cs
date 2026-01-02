using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_AddThingDefCountToList : QuestNode
{
	[NoTranslate]
	public SlateRef<string> name;

	public SlateRef<List<ThingDefCountClass>> thingDefCounts;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		foreach (ThingDefCountClass item in thingDefCounts.GetValue(slate))
		{
			ThingDefCount thingDefCount = item;
			QuestGenUtility.AddToOrMakeList(slate, name.GetValue(slate), thingDefCount);
		}
	}
}
