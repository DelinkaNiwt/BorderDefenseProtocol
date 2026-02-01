using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class DamageWorker_DontHitAllies : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            Thing instigator = dinfo.Instigator;
            if (instigator == null || instigator.HostileTo(thing))
            {
                return base.Apply(dinfo, thing);
            }

            return new DamageResult();
        }
    }
}
