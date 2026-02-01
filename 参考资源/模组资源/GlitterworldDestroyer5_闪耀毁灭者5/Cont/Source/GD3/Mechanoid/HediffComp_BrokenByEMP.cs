using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class HediffCompProperties_BrokenByEMP : HediffCompProperties
    {
        public HediffCompProperties_BrokenByEMP()
        {
            this.compClass = typeof(HediffComp_BrokenByEMP);
        }
    }
    public class HediffComp_BrokenByEMP : HediffComp
    {
        public HediffCompProperties_BrokenByEMP Props
        {
            get
            {
                return (HediffCompProperties_BrokenByEMP)this.props;
            }
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (dinfo.Def == DamageDefOf.EMP)
            {
                parent.pawn.health.RemoveHediff(parent);
            }
        }
    }
}