using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 解析完成的附加视觉层姿态。
    /// </summary>
    internal sealed class ResolvedVisualOverlayPose
    {
        /// <summary>
        /// 当前附加层姿态是否有效。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前附加层使用的贴图。
        /// </summary>
        public Graphic Graphic { get; set; }

        /// <summary>
        /// 当前附加层按人物朝向解析出的最终材质。
        /// </summary>
        public Material DrawMaterial { get; set; }

        /// <summary>
        /// 当前附加层世界绘制位置。
        /// </summary>
        public Vector3 DrawPosition { get; set; }

        /// <summary>
        /// 当前附加层绘制角度。
        /// </summary>
        public float DrawAngle { get; set; }

        /// <summary>
        /// 当前附加层使用的网格种类。
        /// </summary>
        public VisualMeshKind MeshKind { get; set; }

        /// <summary>
        /// 当前附加层绘制缩放。
        /// </summary>
        public float DrawScale { get; set; }
    }
}
