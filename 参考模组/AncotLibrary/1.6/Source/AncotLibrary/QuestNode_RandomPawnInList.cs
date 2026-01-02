using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_RandomPawnInList : QuestNode
{
	[NoTranslate]
	public SlateRef<string> storeAs;

	public SlateRef<IEnumerable<Pawn>> pawnsList;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		List<Pawn> list = new List<Pawn>();
		list.AddRange(pawnsList.GetValue(slate));
		if (!list.NullOrEmpty())
		{
			slate.Set(storeAs.GetValue(slate), list.RandomElementWithFallback());
		}
	}
}
