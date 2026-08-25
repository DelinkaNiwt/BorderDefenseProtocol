using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 解析完成的主视觉姿态。
    /// 它同时携带主贴图、附加层、握持锚点和枪口锚点，保证各点位共用同一宿主基准。
    /// </summary>
    internal sealed class ResolvedVisualPose
    {
        /// <summary>
        /// 当前主视觉姿态是否有效。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前主视觉使用的贴图。
        /// </summary>
        public Graphic Graphic { get; set; }

        /// <summary>
        /// 当前主视觉按人物朝向解析出的最终材质。
        /// </summary>
        public Material DrawMaterial { get; set; }

        /// <summary>
        /// 当前主视觉世界绘制位置。
        /// </summary>
        public Vector3 DrawPosition { get; set; }

        /// <summary>
        /// 当前主视觉绘制角度。
        /// </summary>
        public float DrawAngle { get; set; }

        /// <summary>
        /// 当前主视觉使用的网格种类。
        /// </summary>
        public VisualMeshKind MeshKind { get; set; }

        /// <summary>
        /// 当前主视觉绘制缩放。
        /// </summary>
        public float DrawScale { get; set; }

        /// <summary>
        /// 当前姿态是否发生瞄准镜像。
        /// </summary>
        public bool AimMirror { get; set; }

        /// <summary>
        /// 当前姿态是否发生手侧镜像。
        /// </summary>
        public bool HandMirror { get; set; }

        /// <summary>
        /// 当前解析出的附加层姿态集合。
        /// </summary>
        public IReadOnlyList<ResolvedVisualOverlayPose> OverlayPoses { get; set; }

        /// <summary>
        /// 当前解析出的握持锚点。
        /// </summary>
        public ResolvedGripAnchor GripAnchor { get; set; }

        /// <summary>
        /// 当前解析出的枪口锚点。
        /// </summary>
        public ResolvedMuzzleAnchor MuzzleAnchor { get; set; }

        /// <summary>
        /// 构建一份无效姿态结果。
        /// </summary>
        public static ResolvedVisualPose Invalid()
        {
            return new ResolvedVisualPose
            {
                IsValid = false,
                Graphic = null,
                DrawMaterial = null,
                DrawPosition = Vector3.zero,
                DrawAngle = 0f,
                MeshKind = VisualMeshKind.Plane,
                DrawScale = 1f,
                AimMirror = false,
                HandMirror = false,
                OverlayPoses = new List<ResolvedVisualOverlayPose>(),
                GripAnchor = new ResolvedGripAnchor { IsValid = false },
                MuzzleAnchor = new ResolvedMuzzleAnchor { IsValid = false }
            };
        }
    }
}
