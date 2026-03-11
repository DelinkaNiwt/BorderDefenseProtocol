using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 瞄准结果：宿主合并所有AimIntent后产出，传递给射击阶段
    /// </summary>
    public class AimResult
    {
        public LocalTargetInfo FinalTarget;
        public Vector3 AimPoint;
        public List<IntVec3> AnchorPath;
        public float AnchorSpread;
        public float AccuracyMultiplier = 1f;
        public float ForcedMissRadius;
        public bool Abort;
        public string AbortReason;

        public bool HasGuidedPath => AnchorPath?.Count > 0;  // 修改：使用 null-conditional
    }
}
