using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 视觉姿态解析器。
    /// 它集中处理四朝向位置、AimMirror（瞄准镜像）、HandMirror（手侧镜像）和枪口锚点。
    /// </summary>
    internal sealed class VisualPoseResolver
    {
        /// <summary>
        /// 解析一次主视觉姿态、附加层姿态和枪口锚点。
        /// </summary>
        public ResolvedVisualPose Resolve(VisualPoseRequest request)
        {
            if (!CanResolve(request))
            {
                return ResolvedVisualPose.Invalid();
            }

            PoseCalculation calculation = CalculatePose(request);
            Graphic graphic = request.Preset.ResolveGraphic(request.IsExecutionActive, request.SourceThing);
            if (graphic == null)
            {
                return ResolvedVisualPose.Invalid();
            }

            ResolvedVisualPose resolved = new ResolvedVisualPose
            {
                IsValid = true,
                Graphic = graphic,
                DrawPosition = calculation.DrawPosition,
                DrawAngle = calculation.DrawAngle,
                MeshKind = calculation.MeshKind,
                DrawScale = request.Preset.ResolveDrawScale(),
                AimMirror = calculation.AimMirror,
                HandMirror = calculation.HandMirror,
                OverlayPoses = ResolveOverlayPoses(request, calculation),
                MuzzleAnchor = ResolveMuzzleAnchor(request, calculation)
            };
            return resolved;
        }

        /// <summary>
        /// 只解析枪口锚点。
        /// 它仍通过完整姿态流程进入，保证发射点与贴图位置使用同一套基准。
        /// </summary>
        public bool TryResolveMuzzleAnchor(VisualPoseRequest request, out ResolvedMuzzleAnchor anchor)
        {
            ResolvedVisualPose pose = Resolve(request);
            anchor = pose != null ? pose.MuzzleAnchor : null;
            return anchor != null && anchor.IsValid;
        }

        /// <summary>
        /// 判断请求是否满足最小解析条件。
        /// </summary>
        private static bool CanResolve(VisualPoseRequest request)
        {
            return request != null
                && request.Entry != null
                && request.Preset != null
                && request.PoseSample != null
                && request.PoseSample.IsValid;
        }

        /// <summary>
        /// 计算主贴图位置、角度和镜像状态。
        /// </summary>
        private static PoseCalculation CalculatePose(VisualPoseRequest request)
        {
            bool isSubHand = request.Entry.Side == TriggerSide.Sub;
            PoseOffset offset = request.PoseSample.Facing == Rot4.South || request.PoseSample.Facing == Rot4.North
                ? ResolveSouthNorthOffset(request, isSubHand)
                : ResolveEastWestOffset(request, isSubHand);
            DrawAngleCalculation angle = ResolveDrawAngle(
                request.PoseSample.AimAngle,
                request.EquippedAngleOffset,
                offset.DecorativeAngle,
                offset.HandMirror,
                offset.HandMirrorAllowed,
                offset.FacingMirror);
            Vector3 drawPosition = request.PoseSample.DrawLoc + offset.WorldOffset;
            drawPosition.y += offset.AltitudeOffset;

            return new PoseCalculation
            {
                DrawPosition = drawPosition,
                DrawAngle = angle.DrawAngle,
                MeshKind = angle.MeshKind,
                AimMirror = angle.AimMirror,
                HandMirror = angle.HandMirror
            };
        }

        /// <summary>
        /// 解析南北朝向偏移。
        /// </summary>
        private static PoseOffset ResolveSouthNorthOffset(VisualPoseRequest request, bool isSubHand)
        {
            EquipmentPoseSample sample = request.PoseSample;
            ExpressionVisualSouthNorthPoseConfig pose = request.Preset.ResolveSouthNorthPose();
            float offsetX = pose.DefaultOffset.x;
            float offsetZ = pose.DefaultOffset.z;
            float signX = sample.Facing == Rot4.North ? -1f : 1f;
            float finalX = isSubHand ? -offsetX * signX : offsetX * signX;
            float zAdjust = sample.Facing == Rot4.South ? pose.SouthZAdjust : pose.NorthZAdjust;
            float finalZ = (sample.Facing == Rot4.North ? -offsetZ : offsetZ) + zAdjust;
            bool handMirror = pose.HandMirror
                && !pose.MirrorOnNorth
                && (isSubHand ^ sample.Facing == Rot4.North);
            float altitude = sample.Facing == Rot4.North
                ? -pose.DefaultAltitudeOffset
                : pose.DefaultAltitudeOffset;
            return new PoseOffset
            {
                WorldOffset = new Vector3(finalX, 0f, finalZ),
                AltitudeOffset = altitude,
                DecorativeAngle = pose.DefaultAngle + (isSubHand ? pose.SubHandAngleOffset : 0f),
                HandMirror = handMirror,
                HandMirrorAllowed = pose.HandMirror && !pose.MirrorOnNorth,
                FacingMirror = sample.Facing == Rot4.North && pose.MirrorOnNorth
            };
        }

        /// <summary>
        /// 解析东西朝向偏移。
        /// </summary>
        private static PoseOffset ResolveEastWestOffset(VisualPoseRequest request, bool isSubHand)
        {
            EquipmentPoseSample sample = request.PoseSample;
            ExpressionVisualEastWestPoseConfig pose = request.Preset.ResolveEastWestPose();
            bool isFront = sample.Facing == Rot4.East ? !isSubHand : isSubHand;
            float signBase = sample.Facing == Rot4.East ? 1f : -1f;
            float xDelta = isFront ? -pose.SideDeltaX : pose.SideDeltaX;
            float finalX = signBase * pose.SideBaseX + xDelta;
            float finalZ = isFront ? -pose.SideDeltaZ : pose.SideDeltaZ;
            float altitude = isFront ? pose.FrontAltitudeOffset : pose.BackAltitudeOffset;
            return new PoseOffset
            {
                WorldOffset = new Vector3(finalX, 0f, finalZ),
                AltitudeOffset = altitude,
                DecorativeAngle = pose.DefaultAngle + (isSubHand ? pose.SubHandAngleOffset : 0f),
                HandMirror = false,
                HandMirrorAllowed = pose.HandMirror,
                FacingMirror = false
            };
        }

        /// <summary>
        /// 按原版 DrawEquipmentAiming 规则解析绘制角度和 aim 镜像 mesh。
        /// </summary>
        private static DrawAngleCalculation ResolveDrawAngle(
            float aimAngle,
            float equippedAngleOffset,
            float decorativeAngle,
            bool handMirror,
            bool handMirrorAllowed,
            bool facingMirror)
        {
            float angle = aimAngle - 90f;
            VisualMeshKind meshKind = VisualMeshKind.Plane;
            bool aimMirror = false;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                angle += equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                meshKind = VisualMeshKind.PlaneFlipped;
                aimMirror = true;
                angle -= 180f;
                angle -= equippedAngleOffset;
            }
            else
            {
                angle += equippedAngleOffset;
            }

            angle += meshKind == VisualMeshKind.PlaneFlipped ? -decorativeAngle : decorativeAngle;
            bool appliedHandMirror = false;
            if (handMirrorAllowed && handMirror && IsNearSouthNorthAim(aimAngle))
            {
                meshKind = meshKind == VisualMeshKind.Plane
                    ? VisualMeshKind.PlaneFlipped
                    : VisualMeshKind.Plane;
                angle = -angle;
                appliedHandMirror = true;
            }

            if (facingMirror)
            {
                meshKind = meshKind == VisualMeshKind.Plane
                    ? VisualMeshKind.PlaneFlipped
                    : VisualMeshKind.Plane;
                angle = -angle;
            }

            return new DrawAngleCalculation
            {
                DrawAngle = NormalizeAngle(angle),
                MeshKind = meshKind,
                AimMirror = aimMirror,
                HandMirror = appliedHandMirror
            };
        }

        /// <summary>
        /// 解析全部附加层姿态。
        /// </summary>
        private static IReadOnlyList<ResolvedVisualOverlayPose> ResolveOverlayPoses(
            VisualPoseRequest request,
            PoseCalculation calculation)
        {
            List<ResolvedVisualOverlayPose> result = new List<ResolvedVisualOverlayPose>();
            if (request.Preset.OverlayLayers == null)
            {
                return result;
            }

            for (int i = 0; i < request.Preset.OverlayLayers.Count; i++)
            {
                ExpressionVisualOverlayLayerConfig layer = request.Preset.OverlayLayers[i];
                if (layer == null || !ShouldDrawOverlay(layer, request.IsExecutionActive))
                {
                    continue;
                }

                Graphic graphic = layer.ResolveGraphic(request.IsExecutionActive, request.SourceThing);
                if (graphic == null)
                {
                    continue;
                }

                Vector3 position = calculation.DrawPosition + layer.LocalOffset;
                position.y += layer.AltitudeOffset;
                result.Add(new ResolvedVisualOverlayPose
                {
                    IsValid = true,
                    Graphic = graphic,
                    DrawPosition = position,
                    DrawAngle = NormalizeAngle(calculation.DrawAngle + layer.AngleOffset),
                    MeshKind = calculation.MeshKind,
                    DrawScale = layer.DrawScale > 0f ? layer.DrawScale : request.Preset.ResolveDrawScale()
                });
            }

            return result;
        }

        /// <summary>
        /// 判断指定附加层是否应在当前执行态下绘制。
        /// </summary>
        private static bool ShouldDrawOverlay(ExpressionVisualOverlayLayerConfig layer, bool active)
        {
            if (layer == null)
            {
                return false;
            }

            if (layer.OnlyWhenActive && !active)
            {
                return false;
            }

            if (layer.OnlyWhenInactive && active)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解析枪口锚点。
        /// </summary>
        private static ResolvedMuzzleAnchor ResolveMuzzleAnchor(
            VisualPoseRequest request,
            PoseCalculation calculation)
        {
            ExpressionVisualMuzzleConfig muzzle = request.Preset.ResolveMuzzle();
            if (muzzle == null || !muzzle.IsRangedWeapon)
            {
                return new ResolvedMuzzleAnchor { IsValid = false };
            }

            Vector3 localOffset = ResolveMuzzleLocalOffset(request, muzzle);
            if (IsAimMirrored(request.PoseSample.AimAngle))
            {
                localOffset.x = -localOffset.x;
            }

            Quaternion aimRotation = Quaternion.AngleAxis(request.PoseSample.AimAngle, Vector3.up);
            Vector3 worldOffset = aimRotation * localOffset;
            return new ResolvedMuzzleAnchor
            {
                IsValid = true,
                SourceResultId = request.Entry.ResultId,
                WorldPosition = calculation.DrawPosition + worldOffset + muzzle.ExtraWorldOffset,
                AimAngle = request.PoseSample.AimAngle,
                LocalOffset = localOffset
            };
        }

        /// <summary>
        /// 解析枪口局部偏移。
        /// </summary>
        private static Vector3 ResolveMuzzleLocalOffset(
            VisualPoseRequest request,
            ExpressionVisualMuzzleConfig muzzle)
        {
            if (request.Entry.Side == TriggerSide.Sub && muzzle.HasSubHandMuzzleOffsetOverride)
            {
                return muzzle.SubHandMuzzleOffsetOverride;
            }

            return muzzle.MuzzleOffset;
        }

        /// <summary>
        /// 判断 aimAngle 是否处于原版瞄准镜像半区。
        /// </summary>
        private static bool IsAimMirrored(float aimAngle)
        {
            return aimAngle > 200f && aimAngle < 340f;
        }

        /// <summary>
        /// 判断 aimAngle 是否接近正南或正北。
        /// 只有这个区间才应用手侧镜像，避免斜向射击时枪管朝向反掉。
        /// </summary>
        private static bool IsNearSouthNorthAim(float aimAngle)
        {
            return (aimAngle >= 175f && aimAngle <= 185f)
                || (aimAngle >= 355f || aimAngle <= 5f);
        }

        /// <summary>
        /// 把角度归一到 0 到 360 区间。
        /// </summary>
        private static float NormalizeAngle(float angle)
        {
            float result = angle % 360f;
            return result < 0f ? result + 360f : result;
        }

        /// <summary>
        /// 姿态偏移的中间计算结果。
        /// </summary>
        private sealed class PoseOffset
        {
            /// <summary>
            /// 世界空间基础偏移。
            /// </summary>
            public Vector3 WorldOffset;

            /// <summary>
            /// 高度偏移。
            /// </summary>
            public float AltitudeOffset;

            /// <summary>
            /// 贴图装饰角。
            /// </summary>
            public float DecorativeAngle;

            /// <summary>
            /// 当前手侧是否要求镜像。
            /// </summary>
            public bool HandMirror;

            /// <summary>
            /// 当前姿态分支是否允许手侧镜像生效。
            /// </summary>
            public bool HandMirrorAllowed;

            /// <summary>
            /// 当前姿态分支是否要求做 North 整枪镜像。
            /// </summary>
            public bool FacingMirror;
        }

        /// <summary>
        /// 绘制角度的中间计算结果。
        /// </summary>
        private sealed class DrawAngleCalculation
        {
            /// <summary>
            /// 最终绘制角度。
            /// </summary>
            public float DrawAngle;

            /// <summary>
            /// 最终网格种类。
            /// </summary>
            public VisualMeshKind MeshKind;

            /// <summary>
            /// 是否应用瞄准镜像。
            /// </summary>
            public bool AimMirror;

            /// <summary>
            /// 是否应用手侧镜像。
            /// </summary>
            public bool HandMirror;
        }

        /// <summary>
        /// 主姿态的中间计算结果。
        /// </summary>
        private sealed class PoseCalculation
        {
            /// <summary>
            /// 主贴图绘制位置。
            /// </summary>
            public Vector3 DrawPosition;

            /// <summary>
            /// 主贴图绘制角度。
            /// </summary>
            public float DrawAngle;

            /// <summary>
            /// 主贴图网格种类。
            /// </summary>
            public VisualMeshKind MeshKind;

            /// <summary>
            /// 是否应用瞄准镜像。
            /// </summary>
            public bool AimMirror;

            /// <summary>
            /// 是否应用手侧镜像。
            /// </summary>
            public bool HandMirror;
        }
    }
}
