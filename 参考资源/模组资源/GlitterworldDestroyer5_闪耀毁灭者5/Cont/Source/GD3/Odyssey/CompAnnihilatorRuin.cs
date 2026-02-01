using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
    public class CompAnnihilatorRuin : ThingComp
    {
        public override void Notify_Hacked(Pawn hacker = null)
        {
            if (!GDDefOf.GD3_Annihilator.IsFinished)
            {
                Find.ResearchManager.FinishProject(GDDefOf.GD3_Annihilator, true, hacker, true);
            }
        }
    }

}
