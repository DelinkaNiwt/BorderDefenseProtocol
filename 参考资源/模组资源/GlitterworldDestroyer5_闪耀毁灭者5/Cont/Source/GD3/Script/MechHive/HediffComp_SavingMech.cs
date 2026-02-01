using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class HediffComp_SavingMech : HediffComp
    {
        public bool allowModify = false;

        public bool modified = false;

        public Need_MechEnergy Need => parent.pawn?.needs?.energy;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            if (GDUtility.MissionComponent.savingMechs == null)
            {
                GDUtility.MissionComponent.savingMechs = new List<Pawn>();
            }
            GDUtility.MissionComponent.savingMechs.Add(Pawn);
            Pawn.ageTracker.AgeBiologicalTicks = Rand.Range(60000, 120000);
        }

        public override void CompPostPostRemoved()
        {
            GDUtility.MissionComponent.savingMechs.Remove(Pawn);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Need != null)
            {
                if (parent.pawn.IsHashIntervalTick(250))
                {
                    Need.CurLevel = 100f;
                }
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref allowModify, "allowModify");
            Scribe_Values.Look(ref modified, "modified");
        }
    }
}
