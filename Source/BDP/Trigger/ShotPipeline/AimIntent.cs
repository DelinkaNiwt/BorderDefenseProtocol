using UnityEngine;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 瞄准意图：IShotAimModule产出，多个模块的意图由宿主合并
    /// </summary>
    public struct AimIntent
    {
        // 目标修正
        public LocalTargetInfo? OverrideTarget;
        public Vector3? AimOffset;

        // 引导瞄准
        public IntVec3[] AnchorPoints;  // 修改：使用数组而非 List
        public float? AnchorSpread;

        // 控制标志
        public bool AbortShot;
        public string AbortReason;

        // 精度修正
        public float AccuracyMultiplier;  // 默认1.0
        public float ForcedMissRadius;

        /// <summary>创建默认意图（不修改任何值）</summary>
        public static AimIntent Default => new AimIntent { AccuracyMultiplier = 1f };
    }
}
