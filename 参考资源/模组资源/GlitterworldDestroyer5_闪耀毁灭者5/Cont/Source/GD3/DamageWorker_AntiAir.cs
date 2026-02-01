using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class DamageWorker_AntiAir : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn != null && pawn.Flying)
            {
                return base.Apply(dinfo, thing);
            }

            return new DamageResult();
        }
    }
}
