using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Preview 阶段的中性绘制项。
    /// 它只描述要画什么，不描述业务来源。
    /// </summary>
    public sealed class PreviewDrawItem
    {
        /// <summary>
        /// 当前绘制项类型。
        /// </summary>
        public PreviewDrawItemKind Kind { get; set; }

        /// <summary>
        /// 当前绘制项主起点。
        /// </summary>
        public Vector3 Start { get; set; }

        /// <summary>
        /// 当前绘制项主终点。
        /// </summary>
        public Vector3 End { get; set; }

        /// <summary>
        /// 当前绘制项使用的半径。
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// 当前绘制项使用的格子集合。
        /// </summary>
        public List<IntVec3> Cells { get; } = new List<IntVec3>();

        /// <summary>
        /// 当前绘制项使用的提示文本。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 当前绘制项使用的颜色。
        /// </summary>
        public Color Color { get; set; } = Color.white;
    }
}
