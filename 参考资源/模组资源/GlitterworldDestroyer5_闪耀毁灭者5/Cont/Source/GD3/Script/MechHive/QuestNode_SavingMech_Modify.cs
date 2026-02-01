using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;

namespace GD3
{
	public class QuestNode_SavingMech_Modify : QuestNode
	{
		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Pawn mech = slate.Get<Pawn>("joiner");
			QuestPart_SavingMech_Modify part = new QuestPart_SavingMech_Modify();
			part.inSignal = slate.Get<string>("inSignal");
			part.mech = mech;
			quest.AddPart(part);
		}
	}

}
