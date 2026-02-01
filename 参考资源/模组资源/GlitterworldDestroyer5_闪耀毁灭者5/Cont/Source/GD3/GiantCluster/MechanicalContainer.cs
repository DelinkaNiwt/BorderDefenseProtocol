using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;

namespace GD3
{
    public class MechanicalContainer : Building
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Quest quest = GDUtility.GetQuestOfThing(this);
            if (quest != null && quest.root.defName == "GD_Quest_Cluster_S")
            {
                quest.End(QuestEndOutcome.Success);
            }
            base.Destroy(mode);
        }
    }
}
