using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class PawnRenderNodeWorker_Cane : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            Pawn pawn = parms.pawn;
            if (pawn == null || pawn.DeadOrDowned)
            {
                return false;
            }
            return base.CanDrawNow(node, parms);
        }

        protected override Material GetMaterial(PawnRenderNode node, PawnDrawParms parms)
        {
            Material material = base.GetMaterial(node, parms);
            BlackApocriton blackApocriton = parms.pawn as BlackApocriton;
            int tick = blackApocriton.preventCaneDrawTick;
            if (tick > 0)
            {
                float alpha = tick > 20 ? 0 : 1f - tick / 20f;
                material.SetColor(ShaderPropertyIDs.Color, new Color(1, 1, 1, alpha));
            }
            return material;
        }
    }
}
