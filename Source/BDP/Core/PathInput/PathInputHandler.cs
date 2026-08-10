using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.PathInput
{
    /// <summary>
    /// 通用路径输入处理器 — 纯静态工具类。
    ///
    /// 提供 Shift+锚点 输入的核心逻辑：追加、确认、取消、校验、预览数据、冻结。
    /// 不持有任何状态，所有数据存储在消费层传入的 PathInputState / PathInputConfig 中。
    ///
    /// 消费层：
    ///   - RoutePathModule（毒蛇）— 通过 Targeting/Preview/Confirm 阶段委托调用。
    ///   - Verb_CastAbilityGrasshopper（蚱蜢）— 在 OrderForceTarget/OnGUI 中直接调用。
    /// </summary>
    public static class PathInputHandler
    {
        #region 修饰键

        /// <summary>
        /// 判断输入修饰键中是否包含 Shift。
        /// </summary>
        public static bool HasShiftModifier(TargetingInputModifiers modifiers)
        {
            return (modifiers & TargetingInputModifiers.Shift) != 0;
        }

        #endregion

        #region 基础校验

        /// <summary>
        /// 基础锚点格校验 — 在边界内且可行走。
        /// 消费层可通过 PathInputConfig.AnchorCellValidator 注入额外逻辑（如 Grasshopper 的关门检查）。
        /// </summary>
        public static bool IsValidAnchorCell(Map map, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }
            if (!cell.Walkable(map))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 段通视校验 — 两点之间没有遮挡。
        /// </summary>
        public static bool HasLineOfSight(Map map, IntVec3 from, IntVec3 to)
        {
            if (map == null || !from.IsValid || !to.IsValid)
            {
                return false;
            }
            return GenSight.LineOfSight(from, to, map);
        }

        #endregion

        #region 段起点

        /// <summary>
        /// 解析当前段的起点格子。
        /// 优先级：最后一个锚点 > pawn.Position > IntVec3.Invalid。
        /// </summary>
        public static IntVec3 ResolveSegmentOriginCell(Pawn pawn, PathInputState state)
        {
            if (state != null && state.Anchors.Count > 0)
            {
                PathAnchor lastAnchor = state.Anchors[state.Anchors.Count - 1];
                if (lastAnchor != null)
                {
                    return lastAnchor.ToCell();
                }
            }
            if (pawn != null)
            {
                return pawn.Position;
            }
            return IntVec3.Invalid;
        }

        #endregion

        #region 锚点追加

        /// <summary>
        /// 校验并追加一个锚点。
        /// 返回 null 表示成功；返回非空字符串为拒绝原因。
        ///
        /// 内部执行：上限检查 → 候选有效性 → 格校验 → 段通视 → 自定义校验 → 去重 → 追加 + 清除旧最终目标。
        /// </summary>
        /// <param name="state">当前输入状态（会被修改）。</param>
        /// <param name="config">路径输入配置。</param>
        /// <param name="selectedTarget">玩家点击的目标。</param>
        /// <param name="map">当前地图。</param>
        /// <param name="segmentOrigin">当前段起点（通常由 ResolveSegmentOriginCell 提供）。</param>
        /// <returns>null = 成功；string = 拒绝原因。</returns>
        public static string TryAppendAnchor(
            PathInputState state,
            PathInputConfig config,
            LocalTargetInfo selectedTarget,
            Map map,
            IntVec3 segmentOrigin)
        {
            if (state == null || config == null)
            {
                return "BDP_PathInput_InternalError".Translate();
            }
            if (!selectedTarget.IsValid)
            {
                return "BDP_PathInput_NoTarget".Translate();
            }

            // 上限检查
            if (state.Anchors.Count >= (config.MaxAnchors > 0 ? config.MaxAnchors : 8))
            {
                return "BDP_RoutePath_MaxAnchorsReached".Translate();
            }

            IntVec3 candidateCell = selectedTarget.Cell;

            // 候选格基础校验
            if (!IsValidAnchorCell(map, candidateCell))
            {
                return "BDP_PathInput_InvalidAnchorCell".Translate();
            }

            // 自定义格校验（如关门检查）
            if (config.AnchorCellValidator != null && !config.AnchorCellValidator(map, candidateCell))
            {
                return "BDP_PathInput_InvalidAnchorCell".Translate();
            }

            // 段通视校验
            bool segmentLosOk = true;
            if (segmentOrigin.IsValid)
            {
                if (config.SegmentValidator != null)
                {
                    segmentLosOk = config.SegmentValidator(map, segmentOrigin, candidateCell);
                }
                else
                {
                    segmentLosOk = HasLineOfSight(map, segmentOrigin, candidateCell);
                }
            }
            if (!segmentLosOk)
            {
                return "BDP_PathInput_SegmentBlocked".Translate();
            }

            // 自定义追加前校验
            if (config.AnchorAppendValidator != null)
            {
                string customReject = config.AnchorAppendValidator(map, candidateCell, state);
                if (!string.IsNullOrEmpty(customReject))
                {
                    return customReject;
                }
            }

            // 去重 — 与上一个锚点同格则跳过
            if (state.Anchors.Count > 0)
            {
                PathAnchor lastAnchor = state.Anchors[state.Anchors.Count - 1];
                if (lastAnchor != null && lastAnchor.X == candidateCell.x && lastAnchor.Z == candidateCell.z)
                {
                    return "BDP_PathInput_DuplicateAnchor".Translate();
                }
            }

            // 正式追加
            state.Anchors.Add(PathAnchor.FromCell(candidateCell));

            // 追加锚点后清除已选的最终目标（需要重新确认）
            state.HasFinalTarget = false;
            state.FinalTarget = LocalTargetInfo.Invalid;
            state.FinalIsThing = false;

            return null;
        }

        #endregion

        #region 最终目标确认

        /// <summary>
        /// 校验并确认最终目标。
        /// 返回 null 表示成功（已写入 state.HasFinalTarget / FinalTarget）；返回非空字符串为拒绝原因。
        /// </summary>
        /// <param name="state">当前输入状态（会被修改）。</param>
        /// <param name="config">路径输入配置。</param>
        /// <param name="selectedTarget">玩家点击的最终目标。</param>
        /// <param name="pawn">当前 Pawn。</param>
        /// <param name="segmentOrigin">当前段起点（由 ResolveSegmentOriginCell 提供）。</param>
        /// <param name="map">当前地图。</param>
        /// <returns>null = 成功；string = 拒绝原因。</returns>
        public static string TryConfirmFinalTarget(
            PathInputState state,
            PathInputConfig config,
            LocalTargetInfo selectedTarget,
            Pawn pawn,
            IntVec3 segmentOrigin,
            Map map)
        {
            if (state == null || config == null)
            {
                return "BDP_PathInput_InternalError".Translate();
            }
            if (!selectedTarget.IsValid)
            {
                return "BDP_PathInput_NoTarget".Translate();
            }

            // 目标类型检查
            if (selectedTarget.HasThing && !config.AllowThingFinal)
            {
                return "BDP_PathInput_ThingTargetNotAllowed".Translate();
            }
            if (!selectedTarget.HasThing && !config.AllowGroundFinal)
            {
                return "BDP_PathInput_GroundTargetNotAllowed".Translate();
            }

            // 段通视校验（从当前起点到最后锚点→最终目标）
            if (segmentOrigin.IsValid)
            {
                bool segmentLosOk;
                if (config.SegmentValidator != null)
                {
                    segmentLosOk = config.SegmentValidator(map, segmentOrigin, selectedTarget.Cell);
                }
                else
                {
                    segmentLosOk = HasLineOfSight(map, segmentOrigin, selectedTarget.Cell);
                }
                if (!segmentLosOk)
                {
                    return "BDP_PathInput_SegmentBlocked".Translate();
                }
            }

            // 自定义最终目标校验
            if (config.FinalTargetValidator != null)
            {
                string customReject = config.FinalTargetValidator(selectedTarget, pawn, state);
                if (!string.IsNullOrEmpty(customReject))
                {
                    return customReject;
                }
            }

            // 正式确认
            state.HasFinalTarget = true;
            state.FinalTarget = selectedTarget;
            state.FinalIsThing = selectedTarget.HasThing;

            return null;
        }

        #endregion

        #region 取消

        /// <summary>取消当前路径输入，重置全部状态。</summary>
        public static void Cancel(PathInputState state)
        {
            if (state != null)
            {
                state.Reset();
            }
        }

        #endregion

        #region 预览

        /// <summary>
        /// 构建预览绘制数据。
        /// 产出中性的线段列表 + 计数标签，消费层用自己的绘制 API 落成。
        /// </summary>
        /// <param name="state">当前输入状态。</param>
        /// <param name="pawn">当前 Pawn（用于确定起点）。</param>
        /// <param name="currentTarget">当前鼠标悬停/选中的目标。</param>
        /// <param name="isLastSegmentBlocked">最后一段是否不通（决定末段颜色）。</param>
        public static PathPreviewData BuildPreview(
            PathInputState state,
            Pawn pawn,
            LocalTargetInfo currentTarget,
            bool isLastSegmentBlocked)
        {
            PathPreviewData data = new PathPreviewData
            {
                MaxAnchors = 8,
                Segments = new List<(Vector3 from, Vector3 to, bool isBlocked)>()
            };

            if (state == null || pawn == null)
            {
                return data;
            }

            // 从 pawn 位置起，沿所有锚点画线
            Vector3 lastPoint = pawn.DrawPos;
            for (int i = 0; i < state.Anchors.Count; i++)
            {
                PathAnchor anchor = state.Anchors[i];
                if (anchor == null) continue;
                Vector3 anchorPoint = anchor.ToCell().ToVector3Shifted();
                data.Segments.Add((lastPoint, anchorPoint, false));
                lastPoint = anchorPoint;
            }

            // 末段：从最后锚点（或 pawn）到当前目标
            if (currentTarget.IsValid)
            {
                data.Segments.Add((lastPoint, currentTarget.CenterVector3, isLastSegmentBlocked));
            }

            data.AnchorCount = state.Anchors.Count;
            return data;
        }

        #endregion

        #region 冻结

        /// <summary>
        /// 冻结当前输入状态为确认快照。
        /// 返回深拷贝后的 PathConfirmedData，消费层可用它执行实际的逐段操作。
        /// </summary>
        public static PathConfirmedData Freeze(PathInputState state)
        {
            if (state == null)
            {
                return new PathConfirmedData();
            }

            return new PathConfirmedData
            {
                Anchors = state.CloneTyped().Anchors,
                FinalTarget = state.FinalTarget,
                FinalIsThing = state.FinalIsThing,
                HasManualAnchors = state.Anchors.Count > 0
            };
        }

        #endregion
    }

    /// <summary>
    /// 路径预览绘制数据 — 中性线段列表。
    /// 消费层用各自的绘制 API（PreviewDrawItem / GL 直接绘制）落成。
    /// </summary>
    public sealed class PathPreviewData
    {
        /// <summary>预览线段列表：(起点, 终点, 是否被阻挡)。</summary>
        public List<(Vector3 from, Vector3 to, bool isBlocked)> Segments = new List<(Vector3 from, Vector3 to, bool isBlocked)>();

        /// <summary>当前锚点数量。</summary>
        public int AnchorCount;

        /// <summary>允许的最大锚点数。</summary>
        public int MaxAnchors;

        /// <summary>是否有锚点可显示计数标签。</summary>
        public bool HasAnchors => AnchorCount > 0;
    }

    /// <summary>
    /// 路径确认冻结数据 — 输入确认后的不可变快照。
    /// 消费层读取它来执行实际的逐段操作（逐段飞行 / 逐段跳跃）。
    /// </summary>
    public sealed class PathConfirmedData
    {
        /// <summary>冻结的锚点列表（已去重、已规范化）。</summary>
        public List<PathAnchor> Anchors = new List<PathAnchor>();

        /// <summary>冻结的最终目标。</summary>
        public LocalTargetInfo FinalTarget;

        /// <summary>最终目标是否是 Thing。</summary>
        public bool FinalIsThing;

        /// <summary>是否有玩家手动指定的锚点。</summary>
        public bool HasManualAnchors;

        /// <summary>
        /// 获取完整路径点序列：锚点 + 最终目标。
        /// </summary>
        public List<PathAnchor> GetAllWaypoints()
        {
            List<PathAnchor> waypoints = new List<PathAnchor>();
            for (int i = 0; i < Anchors.Count; i++)
            {
                if (Anchors[i] != null)
                {
                    waypoints.Add(Anchors[i].CloneTyped());
                }
            }
            if (FinalTarget.IsValid)
            {
                waypoints.Add(PathAnchor.FromCell(FinalTarget.Cell));
            }
            return waypoints;
        }
    }
}
