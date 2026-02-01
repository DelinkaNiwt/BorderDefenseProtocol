using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
    public class AnimationWorker_Trigonometric : AnimationWorker_Keyframes
    {
        public override Vector3 OffsetAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            Vector3 offset = base.OffsetAtTick(tick, def, node, part, parms);
            offset *= Mathf.Sin(tick * Mathf.PI / (def.durationTicks / 2f));
            return offset;
        }
    }
}
