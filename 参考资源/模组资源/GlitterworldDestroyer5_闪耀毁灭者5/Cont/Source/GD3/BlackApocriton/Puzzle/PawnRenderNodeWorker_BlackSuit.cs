using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class PawnRenderNodeWorker_BlackSuit : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            if (parms.pawn?.story?.bodyType == BodyTypeDefOf.Hulk)
            {
                offset += new Vector3(0, 0, -0.1f);
            }
            return offset;
        }
    }

    public class PawnRenderNodeWorker_BlackSuit_Epona : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            if (parms.pawn?.story?.bodyType == BodyTypeDefOf.Hulk)
            {
                offset += node.apparel?.def.GetModExtension<ModExtension_DrawOffset>()?.offset ?? Vector3.zero;
            }
            return offset;
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 scale = base.ScaleFor(node, parms);
            if (parms.pawn?.story?.bodyType == BodyTypeDefOf.Hulk)
            {
                ModExtension_DrawOffset ext = node.apparel?.def.GetModExtension<ModExtension_DrawOffset>();
                if (ext != null)
                {
                    scale.x += ext.extraScale;
                    scale.z += ext.extraScale;
                }
            }
            return scale;
        }
    }
}
