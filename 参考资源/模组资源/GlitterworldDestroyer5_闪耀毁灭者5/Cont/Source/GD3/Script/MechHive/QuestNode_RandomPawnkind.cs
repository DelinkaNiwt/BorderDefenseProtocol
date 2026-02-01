using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;

namespace GD3
{
	public class QuestNode_RandomPawnkind : QuestNode
	{
		public SlateRef<List<PawnKindDef>> pawnKinds;

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			List<PawnKindDef> pawnkinds = this.pawnKinds.GetValue(slate);
			PawnKindDef pawnkind = pawnkinds.RandomElement();
			slate.Set("pawnkind", pawnkind);
		}
	}

}
