using UnityEngine;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 解析完成的握持锚点。
    /// 它只表达当前贴图姿态下的握持位置，不参与正式绘制裁决。
    /// </summary>
    internal sealed class ResolvedGripAnchor
    {
        /// <summary>
        /// 当前握持锚点是否有效。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前握持锚点对应的正式源结果标识。
        /// </summary>
        public string SourceResultId { get; set; }

        /// <summary>
        /// 当前握持锚点世界坐标。
        /// </summary>
        public Vector3 WorldPosition { get; set; }

        /// <summary>
        /// 当前握持锚点解算使用的局部偏移。
        /// </summary>
        public Vector3 LocalOffset { get; set; }
    }
}
