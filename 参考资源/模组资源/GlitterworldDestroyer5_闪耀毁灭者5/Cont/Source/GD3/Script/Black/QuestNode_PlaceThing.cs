using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_PlaceThing : QuestNode
	{
		[NoTranslate]
		public SlateRef<string> inSignal;

		public SlateRef<Site> site;

		public SlateRef<ThingDef> thingDef;

		public SlateRef<IntVec3?> offset;

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			QuestPart_PlaceThing questPart_PlaceThing = new QuestPart_PlaceThing();
			questPart_PlaceThing.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
			questPart_PlaceThing.thingDef = thingDef.GetValue(slate);
			questPart_PlaceThing.offset = offset.GetValue(slate) ?? IntVec3.Zero;
			questPart_PlaceThing.site = site.GetValue(slate);
			quest.AddPart(questPart_PlaceThing);
		}
	}
}
