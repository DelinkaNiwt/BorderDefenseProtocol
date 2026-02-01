using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class HediffCompDisappear : HediffComp
    {
        public HediffCompProperties_Disappear Props
        {
            get
            {
                return (HediffCompProperties_Disappear)this.props;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Props.check)
            {
                if (Pawn == null || !Pawn.Spawned || !this.Pawn.RaceProps.IsMechanoid)
                {
                    return;
                }
            }
            List<Thing> list = this.Pawn.Map?.listerThings?.ThingsOfDef(GDDefOf.Mech_BlackApocriton);
            if (list != null && list.Count <= 0)
            {
                Hediff hediff = this.parent;
                this.Pawn.health.hediffSet.hediffs.Remove(hediff);
            }
        }
    }
}