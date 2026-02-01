using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_HyperLinks : QuestNode
	{
		public SlateRef<List<ThingDef>> hyperLinks;

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			List<ThingDef> defs = hyperLinks.GetValue(slate);
			if (!defs.NullOrEmpty())
			{
				QuestPart_Hyperlinks questPart_Hyperlinks = new QuestPart_Hyperlinks();
				questPart_Hyperlinks.thingDefs = defs;
				quest.AddPart(questPart_Hyperlinks);
			}
		}
	}
}
