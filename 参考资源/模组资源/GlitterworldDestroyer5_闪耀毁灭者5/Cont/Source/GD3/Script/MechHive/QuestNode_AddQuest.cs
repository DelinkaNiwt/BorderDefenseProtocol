using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestNode_AddQuest : QuestNode
    {
        public SlateRef<IEnumerable<Pawn>> pawns;

        public SlateRef<QuestScriptDef> def;

        protected override bool TestRunInt(Slate slate)
        {
            return QuestGen_Get.GetMap() != null;
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            FloatRange marketValueRange = new FloatRange(0.5f, 1.0f) * 5000 * Find.Storyteller.difficulty.EffectiveQuestRewardValueFactor;
            QuestPart_AddQuestMechModified questPart_AddQuestMechModified = new QuestPart_AddQuestMechModified();
            questPart_AddQuestMechModified.acceptee = quest.AccepterPawn;
            questPart_AddQuestMechModified.def = def.GetValue(slate);
            questPart_AddQuestMechModified.inSignal = QuestGen.slate.Get<string>("inSignal");
            questPart_AddQuestMechModified.lodgers.AddRange(pawns.GetValue(slate));
            questPart_AddQuestMechModified.marketValueRange = marketValueRange;
            quest.AddPart(questPart_AddQuestMechModified);
            
        }
    }
}
