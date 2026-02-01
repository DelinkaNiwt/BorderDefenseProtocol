using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Building_CentralCharger : Building_MechCharger
    {
        public static Material WireMaterial = MaterialPool.MatFrom("Empty/Empty", ShaderDatabase.Transparent, Color.white);

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (dinfo.Instigator?.def == GDDefOf.Mech_Annihilator)
            {
                absorbed = true;
            }
        }
    }
}
