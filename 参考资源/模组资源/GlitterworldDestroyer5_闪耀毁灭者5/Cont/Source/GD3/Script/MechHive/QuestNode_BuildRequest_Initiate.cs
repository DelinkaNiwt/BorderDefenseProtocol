using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
    public class QuestNode_BuildRequest_Initiate : QuestNode
    {
        public SlateRef<WorldObject> settlement;

        public SlateRef<List<ThingDef>> requestedThingDef;

        public SlateRef<List<int>> requestedThingCount;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_InitiateBuildRequest part = new QuestPart_InitiateBuildRequest
            {
                settlement = settlement.GetValue(slate),
                requestedThingDef = requestedThingDef.GetValue(slate),
                requestedCount = requestedThingCount.GetValue(slate),
                keepAfterQuestEnds = false,
                inSignal = slate.Get<string>("inSignal")
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
        {
            return requestedThingDef.GetValue(slate) != null && requestedThingCount.GetValue(slate).Count == requestedThingDef.GetValue(slate).Count;
        }
    }
}