using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace GD3
{
    public class JobDriver_ModifySavingMech : JobDriver_InteractBase
    {
        public override int InteractingTicks => 60;

        public override bool FailWhen
        {
            get
            {
                return P == null || P.Dead || !P.Spawned || !pawn.CanReserve(P);
            }
        }

        public Pawn P => TargetA.Pawn;
        
        public override EffecterDef Effecter => EffecterDefOf.ConstructMetal;

        public override void TickAction()
        {
            if (P != null)
            {
                P.stances.stunner.StunFor(2, P, false, false, true);
            }
        }

        public override void Action()
        {
            Pawn mech = P;
            HediffWithComps origin = mech.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_SavingMech) as HediffWithComps;
            if (origin != null)
            {
                origin.TryGetComp<HediffComp_SavingMech>().modified = true;
                SoundDefOf.MechSerumUsed.PlayOneShot(P);
            }
            Quest quest = mech.GetQuestOfPawn();
            if (quest != null)
            {
                GDUtility.SendSignal(quest, "Modified");
            }
        }
    }
}
