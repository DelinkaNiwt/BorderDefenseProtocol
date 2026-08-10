using UnityEngine;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 解析完成的枪口锚点。
    /// 它是发射边界可按当前视觉姿态实时解析为发射根点的结果。
    /// </summary>
    internal sealed class ResolvedMuzzleAnchor
    {
        /// <summary>
        /// 当前枪口锚点是否有效。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前枪口锚点对应的正式源结果标识。
        /// </summary>
        public string SourceResultId { get; set; }

        /// <summary>
        /// 当前枪口锚点世界坐标。
        /// </summary>
        public Vector3 WorldPosition { get; set; }

        /// <summary>
        /// 当前枪口解算使用的瞄准角。
        /// </summary>
        public float AimAngle { get; set; }

        /// <summary>
        /// 当前枪口解算使用的局部偏移。
        /// </summary>
        public Vector3 LocalOffset { get; set; }
    }
}
