using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using UnityEngine;

namespace GD3
{
	public class QuestNode_BuildRequest_GetRequestedThing : QuestNode
	{
		[NoTranslate]
		public SlateRef<string> storeThingAs;

		[NoTranslate]
		public SlateRef<string> storeThingCountAs;

		public SlateRef<List<ThingDef>> needDefs;

		public SlateRef<List<int>> needCounts;

		private bool TryFindRandomRequestedThingDef(Map map, out List<ThingDef> thingDef, out List<int> count)
		{
			Slate slate = QuestGen.slate;
			thingDef = needDefs.GetValue(slate);
			count = needCounts.GetValue(slate);
			return true;
		}

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			if (TryFindRandomRequestedThingDef(slate.Get<Map>("map"), out var thingDef, out var count))
			{
				slate.Set(storeThingAs.GetValue(slate), thingDef);
				slate.Set(storeThingCountAs.GetValue(slate), count);
			}
		}

		protected override bool TestRunInt(Slate slate)
		{
			if (TryFindRandomRequestedThingDef(slate.Get<Map>("map"), out var thingDef, out var count))
			{
				slate.Set(storeThingAs.GetValue(slate), thingDef);
				slate.Set(storeThingCountAs.GetValue(slate), count);
				return true;
			}
			return false;
		}
	}

}
