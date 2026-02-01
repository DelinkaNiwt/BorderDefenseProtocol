using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace GD3
{
    public class AnimationWorker_RandomShake : AnimationWorker_Keyframes
    {
        private Vector3 tmpOffset = Vector3.zero;

        private int tmpInt = -1;

        public override Vector3 OffsetAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            Vector3 offset = base.OffsetAtTick(tick, def, node, part, parms);
            if (tmpInt != tick)
            {
                tmpInt = tick;
                tmpOffset = GDUtility.RandomPointInCircle(0.10f);
            }
            return offset + tmpOffset;
        }
    }
}
