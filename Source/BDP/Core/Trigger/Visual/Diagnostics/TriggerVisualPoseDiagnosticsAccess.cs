using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Trigger.Visual;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual.Diagnostics
{
    /// <summary>
    /// Trigger 视觉姿态诊断读取入口。
    /// 它只复用主模组现有已发布投影、运行时状态与姿态解析器生成快照，不引入新的真值路径。
    /// </summary>
    public static class TriggerVisualPoseDiagnosticsAccess
    {
        /// <summary>
        /// 当前诊断入口复用的视觉姿态解析器。
        /// </summary>
        private static readonly VisualPoseResolver VisualPoseResolver = new VisualPoseResolver();

        /// <summary>
        /// 捕获指定 Pawn 当前 Trigger 视觉姿态诊断快照。
        /// 它是纯只读入口，不修改任何运行时状态。
        /// </summary>
        public static TriggerVisualPoseDiagnosticsSnapshot CaptureSnapshot(Pawn pawn)
        {
            TriggerVisualPoseDiagnosticsSnapshot snapshot = new TriggerVisualPoseDiagnosticsSnapshot
            {
                IsAvailable = false,
                UnavailableReason = null,
                PawnLabel = pawn != null ? pawn.LabelShortCap : "无",
                Facing = pawn != null ? pawn.Rotation : Rot4.South,
                ProjectionVersion = 0,
                RuntimeProjectionVersion = 0,
                RelationKind = VisualExpressionRelationKind.None.ToString(),
                HostEquipmentRenderMode = HostEquipmentRenderMode.Keep.ToString(),
                ExecutionFocusPolicy = VisualExecutionFocusPolicy.None.ToString(),
                MuzzleFollowPolicy = VisualMuzzleFollowPolicy.None.ToString(),
                HasPoseSample = false,
                PoseSampleMatchesProjection = false,
                PawnDrawPosition = Vector3.zero,
                DrawLoc = Vector3.zero,
                AimAngle = 0f,
                SampleTick = 0,
                EquippedAngleOffset = ResolveEquippedAngleOffset(pawn),
                HasRecentLaunchOrigin = false,
                RecentLaunchOriginWorld = Vector3.zero,
                RecentLaunchOriginOffsetWorld = Vector3.zero,
                RecentLaunchUsesAbsoluteOriginWorld = false,
                RecentLaunchResultId = null,
                RecentLaunchRootOriginWorld = Vector3.zero,
                RecentLaunchRootSourceKind = TriggerVisualLaunchOriginSourceKind.None.ToString(),
                RecentLaunchRootFailureKind = TriggerVisualLaunchOriginSourceKind.None.ToString(),
                RecentLaunchTick = 0
            };

            TriggerVisualEmissionDiagnosticsSnapshot emissionSnapshot =
                TriggerVisualEmissionDiagnosticsAccess.CaptureSnapshot(pawn);
            if (emissionSnapshot != null && emissionSnapshot.IsAvailable)
            {
                snapshot.RecentLaunchTick = emissionSnapshot.LaunchTick;
                if (emissionSnapshot.LaunchPoints != null)
                {
                    for (int i = 0; i < emissionSnapshot.LaunchPoints.Count; i++)
                    {
                        TriggerVisualEmissionLaunchPointSnapshot point = emissionSnapshot.LaunchPoints[i];
                        if (point == null)
                        {
                            continue;
                        }

                        snapshot.RecentLaunchPoints.Add(point);
                    }
                }

                if (snapshot.RecentLaunchPoints.Count > 0)
                {
                    TriggerVisualEmissionLaunchPointSnapshot lastPoint =
                        snapshot.RecentLaunchPoints[snapshot.RecentLaunchPoints.Count - 1];
                    snapshot.HasRecentLaunchOrigin = true;
                    snapshot.RecentLaunchOriginWorld = lastPoint.ActualLaunchOriginWorld;
                    snapshot.RecentLaunchOriginOffsetWorld = lastPoint.OriginOffsetWorld;
                    snapshot.RecentLaunchUsesAbsoluteOriginWorld = lastPoint.UsesAbsoluteOriginWorld;
                    snapshot.RecentLaunchResultId = lastPoint.ResultId;
                    snapshot.RecentLaunchRootOriginWorld = lastPoint.RootOriginWorld;
                    snapshot.RecentLaunchRootSourceKind = !string.IsNullOrWhiteSpace(lastPoint.RootOriginSourceKind)
                        ? lastPoint.RootOriginSourceKind
                        : TriggerVisualLaunchOriginSourceKind.None.ToString();
                    snapshot.RecentLaunchRootFailureKind = !string.IsNullOrWhiteSpace(lastPoint.RootOriginFailureKind)
                        ? lastPoint.RootOriginFailureKind
                        : TriggerVisualLaunchOriginSourceKind.None.ToString();
                }
            }

            if (pawn == null)
            {
                snapshot.UnavailableReason = "Pawn 为空，无法读取 Trigger 视觉姿态。";
                return snapshot;
            }

            snapshot.PawnDrawPosition = pawn.DrawPos;

            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            if (triggerBody == null)
            {
                snapshot.UnavailableReason = "当前 Pawn 主装备上没有 Trigger 宿主。";
                return snapshot;
            }

            TriggerPresentationState presentation = triggerBody.PublishedPresentationProjection;
            TriggerVisualRuntimeState runtimeState = triggerBody.PublishedVisualRuntimeState;
            VisualExpressionProjection visualProjection = presentation != null ? presentation.VisualProjection : null;
            EquipmentPoseSample sample = runtimeState != null ? runtimeState.EquipmentPoseSample : null;

            snapshot.IsAvailable = true;
            snapshot.ProjectionVersion = presentation != null ? presentation.ProjectionVersion : 0;
            snapshot.RuntimeProjectionVersion = runtimeState != null ? runtimeState.ProjectionVersion : 0;
            snapshot.RelationKind = visualProjection != null
                ? visualProjection.RelationKind.ToString()
                : VisualExpressionRelationKind.None.ToString();
            snapshot.HostEquipmentRenderMode = visualProjection != null
                ? visualProjection.HostEquipmentRenderMode.ToString()
                : HostEquipmentRenderMode.Keep.ToString();
            snapshot.ExecutionFocusPolicy = visualProjection != null
                ? visualProjection.ExecutionFocusPolicy.ToString()
                : VisualExecutionFocusPolicy.None.ToString();
            snapshot.MuzzleFollowPolicy = visualProjection != null
                ? visualProjection.MuzzleFollowPolicy.ToString()
                : VisualMuzzleFollowPolicy.None.ToString();
            snapshot.HasPoseSample = sample != null && sample.IsValid;
            snapshot.PoseSampleMatchesProjection = sample != null
                && sample.IsValidForProjection(snapshot.ProjectionVersion);
            snapshot.DrawLoc = sample != null ? sample.DrawLoc : Vector3.zero;
            snapshot.AimAngle = sample != null ? sample.AimAngle : 0f;
            snapshot.SampleTick = sample != null ? sample.SampleTick : 0;
            snapshot.Facing = sample != null && sample.IsValid ? sample.Facing : pawn.Rotation;

            if (visualProjection == null)
            {
                snapshot.UnavailableReason = "当前没有已发布视觉投影。";
                return snapshot;
            }

            if (visualProjection.ResidentEntries == null || visualProjection.ResidentEntries.Count == 0)
            {
                snapshot.UnavailableReason = snapshot.HasPoseSample
                    ? "当前视觉投影没有常驻条目。"
                    : "当前视觉投影没有常驻条目，且尚未采样到装备姿态。";
                return snapshot;
            }

            if (!snapshot.HasPoseSample)
            {
                snapshot.UnavailableReason = "当前尚未采样到 DrawEquipmentAiming 姿态，需让 Pawn 进入正常持武绘制后再看。";
            }
            else if (!snapshot.PoseSampleMatchesProjection)
            {
                snapshot.UnavailableReason = "当前姿态样本与表现投影版本不一致，已保留原值供定位，但不会解算最终姿态。";
            }

            for (int i = 0; i < visualProjection.ResidentEntries.Count; i++)
            {
                VisualResidentEntry residentEntry = visualProjection.ResidentEntries[i];
                snapshot.Residents.Add(CaptureResidentSnapshot(
                    triggerBody,
                    visualProjection,
                    runtimeState,
                    snapshot,
                    residentEntry));
            }

            return snapshot;
        }

        /// <summary>
        /// 捕获单个常驻视觉条目的诊断快照。
        /// </summary>
        private static TriggerVisualResidentPoseDiagnosticsSnapshot CaptureResidentSnapshot(
            CompTriggerBody triggerBody,
            VisualExpressionProjection visualProjection,
            TriggerVisualRuntimeState runtimeState,
            TriggerVisualPoseDiagnosticsSnapshot rootSnapshot,
            VisualResidentEntry residentEntry)
        {
            TriggerVisualResidentPoseDiagnosticsSnapshot snapshot = new TriggerVisualResidentPoseDiagnosticsSnapshot
            {
                ResultId = residentEntry != null ? residentEntry.ResultId : null,
                Side = residentEntry != null ? residentEntry.Side : TriggerSide.Main,
                SlotIndex = residentEntry != null ? residentEntry.SlotIndex : -1,
                VisualPresetDefName = residentEntry != null ? residentEntry.VisualPresetDefName : null,
                CompositeVisualPresetDefName = residentEntry != null ? residentEntry.CompositeVisualPresetDefName : null,
                ResolvedPresetDefName = null,
                HasPreset = false,
                SourceThingDefName = null,
                SourceThingLabel = null,
                IsExecutionActive = ResolveExecutionActive(visualProjection, runtimeState, residentEntry),
                IsMuzzleActive = ResolveMuzzleActive(visualProjection, runtimeState, residentEntry),
                HasResolvedPose = false,
                ResolvedDrawPosition = Vector3.zero,
                ResolvedDrawAngle = 0f,
                MeshKind = VisualMeshKind.Plane.ToString(),
                DrawScale = 1f,
                AimMirror = false,
                HandMirror = false,
                HasMuzzleAnchor = false,
                MuzzleWorldPosition = Vector3.zero,
                MuzzleLocalOffset = Vector3.zero
            };

            if (residentEntry == null)
            {
                return snapshot;
            }

            string presetDefName = ResolvePresetDefName(visualProjection, residentEntry, runtimeState);
            snapshot.ResolvedPresetDefName = presetDefName;
            ExpressionVisualPresetDef preset = string.IsNullOrWhiteSpace(presetDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(presetDefName, false);
            snapshot.HasPreset = preset != null;

            Thing sourceThing = ResolveSourceThing(triggerBody, residentEntry);
            snapshot.SourceThingDefName = sourceThing != null && sourceThing.def != null ? sourceThing.def.defName : null;
            snapshot.SourceThingLabel = sourceThing != null ? sourceThing.LabelShortCap : null;

            if (preset == null)
            {
                return snapshot;
            }

            FillPresetConfigSnapshot(snapshot, preset);

            if (!rootSnapshot.HasPoseSample || !rootSnapshot.PoseSampleMatchesProjection)
            {
                return snapshot;
            }

            VisualPoseRequest request = new VisualPoseRequest
            {
                Entry = residentEntry,
                Preset = preset,
                RuntimeState = runtimeState,
                PoseSample = runtimeState != null ? runtimeState.EquipmentPoseSample : null,
                SourceThing = sourceThing,
                EquippedAngleOffset = rootSnapshot.EquippedAngleOffset,
                IsExecutionActive = snapshot.IsExecutionActive,
                IsMuzzleActive = snapshot.IsMuzzleActive
            };
            ResolvedVisualPose resolvedPose = VisualPoseResolver.Resolve(request);
            if (resolvedPose == null || !resolvedPose.IsValid)
            {
                return snapshot;
            }

            snapshot.HasResolvedPose = true;
            snapshot.ResolvedDrawPosition = resolvedPose.DrawPosition;
            snapshot.ResolvedDrawAngle = resolvedPose.DrawAngle;
            snapshot.MeshKind = resolvedPose.MeshKind.ToString();
            snapshot.DrawScale = resolvedPose.DrawScale;
            snapshot.AimMirror = resolvedPose.AimMirror;
            snapshot.HandMirror = resolvedPose.HandMirror;
            snapshot.HasMuzzleAnchor = resolvedPose.MuzzleAnchor != null && resolvedPose.MuzzleAnchor.IsValid;
            snapshot.MuzzleWorldPosition = resolvedPose.MuzzleAnchor != null
                ? resolvedPose.MuzzleAnchor.WorldPosition
                : Vector3.zero;
            snapshot.MuzzleLocalOffset = resolvedPose.MuzzleAnchor != null
                ? resolvedPose.MuzzleAnchor.LocalOffset
                : Vector3.zero;
            return snapshot;
        }

        /// <summary>
        /// 复制当前预设的姿态配置原值，便于窗口直接对照参数与表现。
        /// </summary>
        private static void FillPresetConfigSnapshot(
            TriggerVisualResidentPoseDiagnosticsSnapshot snapshot,
            ExpressionVisualPresetDef preset)
        {
            if (snapshot == null || preset == null)
            {
                return;
            }

            ExpressionVisualSouthNorthPoseConfig southNorthPose = preset.ResolveSouthNorthPose();
            ExpressionVisualEastWestPoseConfig eastWestPose = preset.ResolveEastWestPose();

            snapshot.SouthNorthDefaultOffset = southNorthPose.DefaultOffset;
            snapshot.SouthNorthDefaultAngle = southNorthPose.DefaultAngle;
            snapshot.SouthNorthDefaultAltitudeOffset = southNorthPose.DefaultAltitudeOffset;
            snapshot.SouthZAdjust = southNorthPose.SouthZAdjust;
            snapshot.NorthZAdjust = southNorthPose.NorthZAdjust;
            snapshot.SouthNorthSubHandAngleOffset = southNorthPose.SubHandAngleOffset;
            snapshot.SouthNorthHandMirror = southNorthPose.HandMirror;
            snapshot.SouthNorthMirrorOnNorth = southNorthPose.MirrorOnNorth;

            snapshot.SideBaseX = eastWestPose.SideBaseX;
            snapshot.SideDeltaX = eastWestPose.SideDeltaX;
            snapshot.SideDeltaZ = eastWestPose.SideDeltaZ;
            snapshot.FrontAltitudeOffset = eastWestPose.FrontAltitudeOffset;
            snapshot.BackAltitudeOffset = eastWestPose.BackAltitudeOffset;
            snapshot.EastWestDefaultAngle = eastWestPose.DefaultAngle;
            snapshot.EastWestSubHandAngleOffset = eastWestPose.SubHandAngleOffset;
            snapshot.EastWestHandMirror = eastWestPose.HandMirror;
        }

        /// <summary>
        /// 解析当前条目应使用的视觉预设 DefName。
        /// 它与正式绘制路径保持同一关系判断：非 SingleSide 时优先复合预设。
        /// </summary>
        private static string ResolvePresetDefName(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry entry,
            TriggerVisualRuntimeState runtimeState)
        {
            if (visualProjection != null
                && visualProjection.RelationKind != VisualExpressionRelationKind.SingleSide
                && !string.IsNullOrWhiteSpace(entry != null ? entry.CompositeVisualPresetDefName : null))
            {
                return entry.CompositeVisualPresetDefName;
            }

            return entry != null ? entry.VisualPresetDefName : null;
        }

        /// <summary>
        /// 判断当前条目是否命中执行焦点。
        /// </summary>
        private static bool ResolveExecutionActive(
            VisualExpressionProjection visualProjection,
            TriggerVisualRuntimeState runtimeState,
            VisualResidentEntry entry)
        {
            if (visualProjection == null || runtimeState == null || entry == null)
            {
                return false;
            }

            switch (visualProjection.ExecutionFocusPolicy)
            {
                case VisualExecutionFocusPolicy.HostResult:
                    return string.Equals(runtimeState.ActiveHostResultId, entry.ResultId, System.StringComparison.Ordinal);
                case VisualExecutionFocusPolicy.CastResult:
                    return runtimeState.ContainsActiveCastResult(entry.ResultId);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断当前条目是否命中枪口 emit 源焦点。
        /// </summary>
        private static bool ResolveMuzzleActive(
            VisualExpressionProjection visualProjection,
            TriggerVisualRuntimeState runtimeState,
            VisualResidentEntry entry)
        {
            if (visualProjection == null || runtimeState == null || entry == null)
            {
                return false;
            }

            switch (visualProjection.MuzzleFollowPolicy)
            {
                case VisualMuzzleFollowPolicy.HostResult:
                    return string.Equals(runtimeState.ActiveHostResultId, entry.ResultId, System.StringComparison.Ordinal);
                case VisualMuzzleFollowPolicy.CastResult:
                    return runtimeState.ContainsActiveCastResult(entry.ResultId);
                case VisualMuzzleFollowPolicy.EmitSourceResult:
                    return runtimeState.ContainsActiveEmitSourceResult(entry.ResultId);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 根据视觉条目的槽位坐标回到芯片实例。
        /// </summary>
        private static Thing ResolveSourceThing(CompTriggerBody triggerBody, VisualResidentEntry entry)
        {
            if (triggerBody == null || entry == null)
            {
                return null;
            }

            foreach (ITriggerSlotState slot in triggerBody.GetAllSlots())
            {
                if (slot == null || slot.LoadedChip == null)
                {
                    continue;
                }

                if (slot.Side == entry.Side && slot.Index == entry.SlotIndex)
                {
                    return slot.LoadedChip;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取宿主装备 ThingDef 上的 equippedAngleOffset。
        /// </summary>
        private static float ResolveEquippedAngleOffset(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.def != null
                ? pawn.equipment.Primary.def.equippedAngleOffset
                : 0f;
        }
    }
}
