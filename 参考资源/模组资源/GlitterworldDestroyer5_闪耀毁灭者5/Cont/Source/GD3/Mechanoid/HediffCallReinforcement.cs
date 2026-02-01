using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
    public class HediffCallReinforcement : HediffDrawer
    {
        public override void PostTick()
        {
            base.PostTick();
            if (pawn.IsHashIntervalTick(20))
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            if (pawn.IsHashIntervalTick(400) && (pawn.mindState.enemyTarget != null || pawn.mindState.meleeThreat != null))
            {
                Ability ability = pawn.abilities?.GetAbility(GDDefOf.MechCallReinforcement);
                if (ability != null && ability.AICanTargetNow(pawn) && !ability.Casting)
                {
                    Job job = ability.GetJob(pawn, pawn);
                    if (job != null)
                    {
                        pawn.stances.CancelBusyStanceHard();
                        pawn.jobs.TryTakeOrderedJob(job);
                    }
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Pawn_AbilityTracker abilities = pawn.abilities;
            if (abilities != null && abilities.GetAbility(GDDefOf.MechCallReinforcement) == null)
            {
                abilities.GainAbility(GDDefOf.MechCallReinforcement);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            Pawn_AbilityTracker abilities = pawn.abilities;
            if (abilities != null && abilities.GetAbility(GDDefOf.MechCallReinforcement) != null)
            {
                abilities.RemoveAbility(GDDefOf.MechCallReinforcement);
            }
        }
    }
}
