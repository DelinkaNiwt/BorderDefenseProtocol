using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_LordJob_ExitMapBest : QuestNode
{
	public SlateRef<Pawn> pawn;

	public SlateRef<List<Pawn>> pawns;

	public SlateRef<FactionDef> faction;

	public SlateRef<string> inSignal;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		List<Pawn> list = new List<Pawn>();
		if (pawn.GetValue(slate) != null)
		{
			list.Add(pawn.GetValue(slate));
		}
		if (pawns.GetValue(slate) != null)
		{
			list.AddRange(pawns.GetValue(slate));
		}
		QuestPart_LordJob_ExitMapBest questPart_LordJob_ExitMapBest = new QuestPart_LordJob_ExitMapBest();
		questPart_LordJob_ExitMapBest.pawns = list;
		questPart_LordJob_ExitMapBest.faction = Find.FactionManager.FirstFactionOfDef(faction.GetValue(slate));
		questPart_LordJob_ExitMapBest.map = list.FirstOrDefault()?.MapHeld ?? null;
		questPart_LordJob_ExitMapBest.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
		QuestGen.quest.AddPart(questPart_LordJob_ExitMapBest);
	}
}
