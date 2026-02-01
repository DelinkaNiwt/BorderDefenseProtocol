using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class Verb_AntiAirShoot : Verb_Shoot
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
            {
                Projectile proj = target.Thing as Projectile;
                if (proj == null || !proj.def.projectile.flyOverhead)
                {
                    return false;
                }
            }
            else
            {
                if (!pawn.Flying)
                {
                    return false;
                }
            }
            return base.ValidateTarget(target, showMessages);
        }
    }
}
