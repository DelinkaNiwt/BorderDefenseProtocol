using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 视觉姿态解析器。
    /// 它集中处理四朝向位置、AimMirror（瞄准镜像）、HandMirror（手侧镜像）以及视觉锚点。
    /// </summary>
    internal sealed class VisualPoseResolver
    {
        /// <summary>
        /// 解析一次主视觉姿态、附加层姿态、握持锚点和枪口锚点。
        /// </summary>
        public ResolvedVisualPose Resolve(VisualPoseRequest request)
        {
            if (!CanResolve(request))
            {
                return ResolvedVisualPose.Invalid();
            }

            PoseCalculation calculation = CalculatePose(request);
            WeaponVisualActionStage weaponStage = ResolveWeaponStage(request);
            Graphic graphic = ResolveMainGraphic(request, weaponStage);
            if (graphic == null)
            {
                return ResolvedVisualPose.Invalid();
            }

            AlignDrawPositionToGrip(request, calculation);
            ResolvedVisualPose resolved = new ResolvedVisualPose
            {
                IsValid = true,
                Graphic = graphic,
                DrawMaterial = ResolveDrawMaterial(
                    graphic,
                    request.PoseSample.Facing,
                    request.SourceThing),
                DrawPosition = calculation.DrawPosition,
                DrawAngle = calculation.DrawAngle,
                MeshKind = calculation.MeshKind,
                DrawScale = request.Preset.ResolveDrawScale(),
                AimMirror = calculation.AimMirror,
                HandMirror = calculation.HandMirror,
                OverlayPoses = ResolveOverlayPoses(request, calculation, weaponStage),
                GripAnchor = ResolveGripAnchor(request, calculation),
                MuzzleAnchor = ResolveMuzzleAnchor(request, calculation)
            };
            return resolved;
        }

        /// <summary>
        /// 按 RimWorld 原版装备姿态解析单武器主贴图、附加层和枪口锚点。
        /// 此入口不读取双武器偏移、手侧镜像或握持姿态原点，保证单武器只替换贴图。
        /// </summary>
        public ResolvedVisualPose ResolveTextureOnly(VisualPoseRequest request)
        {
            if (!CanResolve(request))
            {
                return ResolvedVisualPose.Invalid();
            }

            PoseCalculation calculation = CalculateVanillaPose(request);
            WeaponVisualActionStage weaponStage = ResolveWeaponStage(request);
            Graphic graphic = ResolveMainGraphic(request, weaponStage);
            if (graphic == null)
            {
                return ResolvedVisualPose.Invalid();
            }

            return new ResolvedVisualPose
            {
                IsValid = true,
                Graphic = graphic,
                DrawMaterial = ResolveDrawMaterial(
                    graphic,
                    request.PoseSample.Facing,
                    request.SourceThing),
                DrawPosition = calculation.DrawPosition,
                DrawAngle = calculation.DrawAngle,
                MeshKind = calculation.MeshKind,
                DrawScale = 1f,
                AimMirror = calculation.AimMirror,
                HandMirror = false,
                OverlayPoses = ResolveOverlayPoses(request, calculation, weaponStage),
                GripAnchor = new ResolvedGripAnchor { IsValid = false },
                MuzzleAnchor = ResolveMuzzleAnchor(request, calculation)
            };
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
        /// 按 RimWorld 原版装备姿态只解析单武器枪口锚点。
        /// </summary>
        public bool TryResolveTextureOnlyMuzzleAnchor(
            VisualPoseRequest request,
            out ResolvedMuzzleAnchor anchor)
        {
            ResolvedVisualPose pose = ResolveTextureOnly(request);
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
        /// 按人物朝向解析最终绘制材质。
        /// Graphic_Single（单向贴图）会自然返回同一材质，Graphic_Multi（多朝向贴图）则选择对应方向资源。
        /// </summary>
        private static Material ResolveDrawMaterial(Graphic graphic, Rot4 facing, Thing sourceThing)
        {
            return graphic != null ? graphic.MatAt(facing, sourceThing) : null;
        }

        /// <summary>
        /// 读取请求携带的武器动作阶段；旧调用方未提供快照时按空闲态处理。
        /// </summary>
        private static WeaponVisualActionStage ResolveWeaponStage(VisualPoseRequest request)
        {
            return request?.WeaponStageSnapshot != null
                ? request.WeaponStageSnapshot.Stage
                : WeaponVisualActionStage.Idle;
        }

        /// <summary>
        /// 解析主贴图：姿态来自基础预设，声明了局部覆盖时只替换主 GraphicData。
        /// </summary>
        private static Graphic ResolveMainGraphic(
            VisualPoseRequest request,
            WeaponVisualActionStage weaponStage)
        {
            ExpressionVisualPresetDef graphicPreset = request.GraphicOverridePreset ?? request.Preset;
            return graphicPreset != null
                ? graphicPreset.ResolveGraphic(
                    request.IsExecutionActive,
                    weaponStage,
                    request.SourceThing)
                : null;
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
                ResolveAimAngle(request),
                request.EquippedAngleOffset,
                offset.DecorativeAngle,
                offset.HandMirror,
                offset.HandMirrorAllowed,
                offset.ForceHandMirror,
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
        /// 解析当前视觉预设实际用于贴图旋转的瞄准角。
        /// 限幅模式保留连续目标位置，但把贴图旋转压回人物当前四向的原版持械基准附近。
        /// </summary>
        private static float ResolveAimAngle(VisualPoseRequest request)
        {
            if (request?.PoseSample == null || request.Preset == null)
            {
                return 0f;
            }

            float limit = Mathf.Clamp(request.Preset.AimRotationLimit, 0f, 45f);
            if (limit <= 0f)
            {
                return request.PoseSample.AimAngle;
            }

            float facingCenter = ResolveFacingCenterAngle(request.PoseSample.Facing);
            float localDelta = Mathf.Clamp(
                Mathf.DeltaAngle(facingCenter, request.PoseSample.AimAngle),
                -45f,
                45f);
            float carriedAimAngle = request.PoseSample.Facing == Rot4.West ? 217f : 143f;
            return NormalizeAngle(carriedAimAngle + localDelta * limit / 45f);
        }

        /// <summary>
        /// 把 RimWorld 四向朝向转换为目标方向中心角。
        /// </summary>
        private static float ResolveFacingCenterAngle(Rot4 facing)
        {
            switch (facing.AsInt)
            {
                case 1:
                    return 90f;
                case 2:
                    return 180f;
                case 3:
                    return 270f;
                case 0:
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 复现 RimWorld 原版 DrawEquipmentAiming（瞄准装备绘制）的位置、角度与网格镜像。
        /// </summary>
        private static PoseCalculation CalculateVanillaPose(VisualPoseRequest request)
        {
            DrawAngleCalculation angle = ResolveDrawAngle(
                request.PoseSample.AimAngle,
                request.EquippedAngleOffset,
                0f,
                false,
                false,
                false,
                false);
            return new PoseCalculation
            {
                DrawPosition = request.PoseSample.DrawLoc,
                DrawAngle = angle.DrawAngle,
                MeshKind = angle.MeshKind,
                AimMirror = angle.AimMirror,
                HandMirror = false
            };
        }

        /// <summary>
        /// 解析南北朝向偏移。
        /// </summary>
        private static PoseOffset ResolveSouthNorthOffset(VisualPoseRequest request, bool isSubHand)
        {
            EquipmentPoseSample sample = request.PoseSample;
            ExpressionVisualSouthNorthPoseConfig pose = request.Preset.ResolveSouthNorthPose();
            bool isAnyExecutionActive = IsAnyExecutionActive(request);
            // X 是左右手之间的无方向分离距离；屏幕方向只由手侧和人物朝向裁定，避免作者用正负号颠倒主副手语义。
            float sideDistanceX = Mathf.Abs(pose.DefaultOffset.x);
            float offsetZ = pose.DefaultOffset.z;
            // Main（主侧）恒指右手、Sub（副侧）恒指左手；South 面向玩家时身体右手投影到屏幕左侧，North 时反转。
            float signX = sample.Facing == Rot4.South ? -1f : 1f;
            float finalX = (isSubHand ? -sideDistanceX : sideDistanceX) * signX;
            float zAdjust = sample.Facing == Rot4.South ? pose.SouthZAdjust : pose.NorthZAdjust;
            float finalZ = (sample.Facing == Rot4.North ? -offsetZ : offsetZ) + zAdjust;
            bool handMirror = pose.HandMirror
                && !pose.MirrorOnNorth
                // 静默专用镜像不能由“当前条目是否执行”裁定；单侧攻击期间两把武器都必须退出静默姿态。
                && (!pose.HandMirrorOnlyWhenIdle || !isAnyExecutionActive)
                // 镜像恒跟随屏幕左侧：South（南向）是主手，North（北向）是副手。
                && (isSubHand ^ sample.Facing == Rot4.South);
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
                ForceHandMirror = pose.HandMirrorOnlyWhenIdle && !isAnyExecutionActive,
                FacingMirror = sample.Facing == Rot4.North && pose.MirrorOnNorth
            };
        }

        /// <summary>
        /// 判断当前双武器视觉是否处于任意攻击执行中。
        /// 条目执行态只表示某一侧命中执行焦点；整体静默必须读取整轮运行时状态。
        /// </summary>
        private static bool IsAnyExecutionActive(VisualPoseRequest request)
        {
            // 只认整轮运行时状态，不能用 request.IsExecutionActive（单条执行焦点）回退，
            // 否则单侧攻击时两把武器会再次分裂成攻击姿态与静默姿态。
            return request?.RuntimeState?.HasExecutionState == true;
        }

        /// <summary>
        /// 解析东西朝向偏移。
        /// </summary>
        private static PoseOffset ResolveEastWestOffset(VisualPoseRequest request, bool isSubHand)
        {
            EquipmentPoseSample sample = request.PoseSample;
            ExpressionVisualEastWestPoseConfig pose = request.Preset.ResolveEastWestPose();
            bool facingWest = sample.Facing == Rot4.West;
            bool isFront = pose.MainHandAlwaysFront
                ? !isSubHand
                : facingWest ? isSubHand : !isSubHand;
            // 可选策略把朝西基础镜像也纳入额外翻转，使最终外观恒为主手原图、副手镜像。
            bool handMirror = pose.HandMirror
                && (pose.FinalMirrorByHandOnly ? isSubHand ^ facingWest : isSubHand);
            float signBase = sample.Facing == Rot4.East ? 1f : -1f;
            float xDelta = isFront ? -pose.SideDeltaX : pose.SideDeltaX;
            // X 分离量与共同基准一并随 East/West 反转，保证两侧观察时前景手都更靠近人物。
            float finalX = signBase * (pose.SideBaseX + xDelta);
            // Z 先应用两手共同的侧身基准，再叠加前景低、背景高的透视分离。
            float finalZ = pose.SideBaseZ + (isFront ? -pose.SideDeltaZ : pose.SideDeltaZ);
            float altitude = isFront ? pose.FrontAltitudeOffset : pose.BackAltitudeOffset;
            return new PoseOffset
            {
                WorldOffset = new Vector3(finalX, 0f, finalZ),
                AltitudeOffset = altitude,
                DecorativeAngle = pose.DefaultAngle + (isSubHand ? pose.SubHandAngleOffset : 0f),
                HandMirror = handMirror,
                HandMirrorAllowed = pose.HandMirror,
                ForceHandMirror = pose.HandMirror,
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
            bool forceHandMirror,
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
            if (handMirrorAllowed
                && handMirror
                && (forceHandMirror || IsNearSouthNorthAim(aimAngle)))
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
            PoseCalculation calculation,
            WeaponVisualActionStage weaponStage)
        {
            List<ResolvedVisualOverlayPose> result = new List<ResolvedVisualOverlayPose>();
            ExpressionVisualPresetDef overlayPreset = request.GraphicOverridePreset ?? request.Preset;
            List<ExpressionVisualOverlayLayerConfig> overlayLayers =
                overlayPreset.ResolveOverlayLayers(weaponStage);
            if (overlayLayers == null)
            {
                return result;
            }

            for (int i = 0; i < overlayLayers.Count; i++)
            {
                ExpressionVisualOverlayLayerConfig layer = overlayLayers[i];
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
                    DrawMaterial = ResolveDrawMaterial(
                        graphic,
                        request.PoseSample.Facing,
                        request.SourceThing),
                    DrawPosition = position,
                    DrawAngle = NormalizeAngle(calculation.DrawAngle + layer.AngleOffset),
                    MeshKind = calculation.MeshKind,
                    DrawScale = layer.DrawScale > 0f ? layer.DrawScale : overlayPreset.ResolveDrawScale()
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
        /// 按最终角度和网格镜像，把姿态目标位置解释为握持点位置并反推主贴图中心。
        /// 未显式开启时不改变既有以贴图中心为原点的行为。
        /// </summary>
        private static void AlignDrawPositionToGrip(
            VisualPoseRequest request,
            PoseCalculation calculation)
        {
            ExpressionVisualGripConfig grip = request.Preset.ResolveGrip();
            if (grip == null || !grip.UseAsPoseOrigin)
            {
                return;
            }

            calculation.DrawPosition -= TransformGraphicLocalOffset(
                grip.GripOffset,
                calculation);
        }

        /// <summary>
        /// 按最终贴图角度和网格镜像解析握持锚点。
        /// 该点只跟随视觉姿态，不反向改变主贴图绘制位置。
        /// </summary>
        private static ResolvedGripAnchor ResolveGripAnchor(
            VisualPoseRequest request,
            PoseCalculation calculation)
        {
            ExpressionVisualGripConfig grip = request.Preset.ResolveGrip();
            if (grip == null)
            {
                return new ResolvedGripAnchor { IsValid = false };
            }

            Vector3 localOffset = grip.GripOffset;
            Vector3 worldOffset = TransformGraphicLocalOffset(localOffset, calculation);
            return new ResolvedGripAnchor
            {
                IsValid = true,
                SourceResultId = request.Entry.ResultId,
                WorldPosition = calculation.DrawPosition + worldOffset,
                LocalOffset = localOffset
            };
        }

        /// <summary>
        /// 把贴图局部点转换为世界偏移。
        /// 配置 Z 对应贴图水平方向，配置 X 对应贴图垂直方向；翻转网格会反转贴图水平方向。
        /// </summary>
        private static Vector3 TransformGraphicLocalOffset(
            Vector3 localOffset,
            PoseCalculation calculation)
        {
            float meshForward = calculation.MeshKind == VisualMeshKind.PlaneFlipped
                ? -localOffset.z
                : localOffset.z;
            Vector3 meshLocalOffset = new Vector3(meshForward, localOffset.y, -localOffset.x);
            Quaternion visualRotation = Quaternion.AngleAxis(calculation.DrawAngle, Vector3.up);
            return visualRotation * meshLocalOffset;
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
            Vector3 worldOffset = TransformGraphicLocalOffset(localOffset, calculation);
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
            /// 当前姿态是否允许跳过正南北瞄准角门槛，强制执行已裁定的手侧镜像。
            /// </summary>
            public bool ForceHandMirror;

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
