using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
    public class AnimationWorker_Jump : AnimationWorker_Keyframes
    {
        public override Vector3 OffsetAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            Vector3 offset = base.OffsetAtTick(tick, def, node, part, parms);
            offset *= (float)(((3 + 2 * Math.Sqrt(2)) / 9600) * Math.Pow(tick, 2) + ((-1 - Math.Sqrt(2)) / 40) * tick);
            return offset;
        }
    }

    public class AnimationWorker_Jump_Light : AnimationWorker_Keyframes
    {
        public override Vector3 OffsetAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            Vector3 offset = base.OffsetAtTick(tick, def, node, part, parms);
            offset *= (float)((1.0 / 2400) * Math.Pow(tick, 2) + (-1.0 / 20) * tick);
            return offset;
        }
    }
}
