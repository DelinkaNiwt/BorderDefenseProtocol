using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual.Diagnostics
{
    /// <summary>
    /// Trigger 视觉发射原点诊断读取入口。
    /// 它缓存最近一次发射批次里的全部真实发射点，并为每一发保留对应理论中心原点。
    /// </summary>
    public static class TriggerVisualEmissionDiagnosticsAccess
    {
        /// <summary>
        /// 最近一批发射点在诊断层中的保留时长。
        /// 保留几秒可用于暂停截图，而不会让旧批次长期污染当前判读。
        /// </summary>
        private const int RetainTicks = 240;

        /// <summary>
        /// 按 Pawn thingIDNumber 缓存的最近一批发射诊断记录。
        /// 每个 Pawn 同时只保留一批，新的批次会整体覆盖旧批次。
        /// </summary>
        private static readonly Dictionary<int, LaunchBatchRecord> BatchesByPawnId =
            new Dictionary<int, LaunchBatchRecord>();

        /// <summary>
        /// 开始指定 Pawn 当前这一轮 burst 的诊断批次。
        /// 新一轮 burst 开始时应整体替换上一轮缓存，但单点自身的保留时长仍按各自 launchTick 自然衰减。
        /// </summary>
        internal static void BeginBurstBatch(Pawn pawn, string attackInstanceId)
        {
            if (!DebugSettings.godMode || pawn == null || pawn.thingIDNumber <= 0)
            {
                return;
            }

            BatchesByPawnId[pawn.thingIDNumber] = new LaunchBatchRecord
            {
                AttackInstanceId = attackInstanceId,
                BurstStartTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0,
                LatestLaunchTick = 0
            };
        }

        /// <summary>
        /// 记录指定 Pawn 当前批次中的一条发射点。
        /// 同一轮 burst 中的多发 projectile 会持续追加到当前批次，而不会被每枪覆盖。
        /// </summary>
        internal static void RecordLaunchOrigin(
            Pawn pawn,
            string attackInstanceId,
            string resultId,
            Vector3 rootOriginWorld,
            Vector3 theoreticalCenterOriginWorld,
            Vector3 actualLaunchOriginWorld,
            Vector3 originOffsetWorld,
            string rootOriginSourceKind,
            string rootOriginFailureKind,
            bool usesAbsoluteOriginWorld,
            int projectionVersion,
            int poseSampleTick)
        {
            if (!DebugSettings.godMode || pawn == null || pawn.thingIDNumber <= 0)
            {
                return;
            }

            int launchTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!BatchesByPawnId.TryGetValue(pawn.thingIDNumber, out LaunchBatchRecord batch)
                || batch == null
                || !string.Equals(batch.AttackInstanceId, attackInstanceId, StringComparison.Ordinal))
            {
                batch = new LaunchBatchRecord
                {
                    AttackInstanceId = attackInstanceId,
                    BurstStartTick = launchTick,
                    LatestLaunchTick = 0
                };
                BatchesByPawnId[pawn.thingIDNumber] = batch;
            }

            batch.LatestLaunchTick = launchTick;
            batch.LaunchPoints.Add(new LaunchPointRecord
            {
                ResultId = resultId,
                RootOriginWorld = rootOriginWorld,
                TheoreticalCenterOriginWorld = theoreticalCenterOriginWorld,
                ActualLaunchOriginWorld = actualLaunchOriginWorld,
                OriginOffsetWorld = originOffsetWorld,
                RootOriginSourceKind = rootOriginSourceKind,
                RootOriginFailureKind = rootOriginFailureKind,
                UsesAbsoluteOriginWorld = usesAbsoluteOriginWorld,
                LaunchTick = launchTick,
                ProjectionVersion = projectionVersion,
                PoseSampleTick = poseSampleTick
            });
        }

        /// <summary>
        /// 读取指定 Pawn 当前仍然可用的最近一批发射点快照。
        /// 若批次过期、Pawn 无效或当前不在 DevMode，则返回空快照。
        /// </summary>
        public static TriggerVisualEmissionDiagnosticsSnapshot CaptureSnapshot(Pawn pawn)
        {
            TriggerVisualEmissionDiagnosticsSnapshot snapshot = new TriggerVisualEmissionDiagnosticsSnapshot
            {
                IsAvailable = false,
                AttackInstanceId = null,
                LaunchTick = 0,
                LaunchPoints = new List<TriggerVisualEmissionLaunchPointSnapshot>()
            };

            if (!DebugSettings.godMode || pawn == null || pawn.thingIDNumber <= 0)
            {
                return snapshot;
            }

            if (!BatchesByPawnId.TryGetValue(pawn.thingIDNumber, out LaunchBatchRecord batch) || batch == null)
            {
                return snapshot;
            }

            PruneExpiredPoints(batch);
            if (batch.LaunchPoints.Count == 0)
            {
                BatchesByPawnId.Remove(pawn.thingIDNumber);
                return snapshot;
            }

            snapshot.IsAvailable = batch.LaunchPoints.Count > 0;
            snapshot.AttackInstanceId = batch.AttackInstanceId;
            snapshot.LaunchTick = batch.LatestLaunchTick;
            for (int i = 0; i < batch.LaunchPoints.Count; i++)
            {
                LaunchPointRecord point = batch.LaunchPoints[i];
                if (point == null)
                {
                    continue;
                }

                snapshot.LaunchPoints.Add(new TriggerVisualEmissionLaunchPointSnapshot
                {
                    ResultId = point.ResultId,
                    RootOriginWorld = point.RootOriginWorld,
                    TheoreticalCenterOriginWorld = point.TheoreticalCenterOriginWorld,
                    ActualLaunchOriginWorld = point.ActualLaunchOriginWorld,
                    OriginOffsetWorld = point.OriginOffsetWorld,
                    RootOriginSourceKind = point.RootOriginSourceKind,
                    RootOriginFailureKind = point.RootOriginFailureKind,
                    UsesAbsoluteOriginWorld = point.UsesAbsoluteOriginWorld,
                    LaunchTick = point.LaunchTick,
                    ProjectionVersion = point.ProjectionVersion,
                    PoseSampleTick = point.PoseSampleTick
                });
            }

            return snapshot;
        }

        /// <summary>
        /// 清理当前批次中已经超过保留时长的单个发射点。
        /// 这里按点位自己的 launchTick 判断过期，因此同一轮 burst 的旧点会自然先于新点消失。
        /// </summary>
        private static void PruneExpiredPoints(LaunchBatchRecord batch)
        {
            if (batch == null)
            {
                return;
            }

            if (Find.TickManager == null || batch.LaunchPoints == null || batch.LaunchPoints.Count == 0)
            {
                return;
            }

            batch.LaunchPoints.RemoveAll(point =>
                point == null
                || point.LaunchTick <= 0
                || Find.TickManager.TicksGame - point.LaunchTick > RetainTicks);

            batch.LatestLaunchTick = 0;
            for (int i = 0; i < batch.LaunchPoints.Count; i++)
            {
                LaunchPointRecord point = batch.LaunchPoints[i];
                if (point != null && point.LaunchTick > batch.LatestLaunchTick)
                {
                    batch.LatestLaunchTick = point.LaunchTick;
                }
            }
        }

        /// <summary>
        /// 单个 Pawn 最近一批发射点的运行时缓存记录。
        /// 它是纯诊断数据容器，不向外暴露行为。
        /// </summary>
        private sealed class LaunchBatchRecord
        {
            /// <summary>
            /// 本批次所属的攻击实例标识。
            /// </summary>
            public string AttackInstanceId { get; set; }

            /// <summary>
            /// 本轮 burst 批次开始时的游戏 tick。
            /// </summary>
            public int BurstStartTick { get; set; }

            /// <summary>
            /// 当前批次里最近一条仍然保留的发射点 tick。
            /// </summary>
            public int LatestLaunchTick { get; set; }

            /// <summary>
            /// 本批次包含的全部发射点。
            /// </summary>
            public List<LaunchPointRecord> LaunchPoints { get; } = new List<LaunchPointRecord>();
        }

        /// <summary>
        /// 单发 projectile 的运行时诊断缓存。
        /// 它同时保留理论中心原点与真实发射原点，便于区分“源点散布前后”。
        /// </summary>
        private sealed class LaunchPointRecord
        {
            /// <summary>
            /// 本发 projectile 所属的正式结果标识。
            /// </summary>
            public string ResultId { get; set; }

            /// <summary>
            /// 本发 projectile 当前采用的根原点。
            /// 理论中心原点与真实发射点都应建立在它之上。
            /// </summary>
            public Vector3 RootOriginWorld { get; set; }

            /// <summary>
            /// 本发 projectile 的理论中心原点。
            /// 它代表不考虑本发起点散布时的中心发射位置。
            /// </summary>
            public Vector3 TheoreticalCenterOriginWorld { get; set; }

            /// <summary>
            /// 本发 projectile 最终实际使用的真实发射原点。
            /// </summary>
            public Vector3 ActualLaunchOriginWorld { get; set; }

            /// <summary>
            /// 本发 projectile 计划声明的世界偏移量。
            /// </summary>
            public Vector3 OriginOffsetWorld { get; set; }

            /// <summary>
            /// 本发 projectile 是否直接使用绝对世界原点作为理论中心。
            /// </summary>
            public bool UsesAbsoluteOriginWorld { get; set; }

            /// <summary>
            /// 本发 projectile 实际记录时的发射 tick。
            /// 单点保留时长按它自己计算，而不是按整批统一计算。
            /// </summary>
            public int LaunchTick { get; set; }

            /// <summary>
            /// 本发 projectile 实际采用的根原点来源类型。
            /// </summary>
            public string RootOriginSourceKind { get; set; }

            /// <summary>
            /// 当根原点没有来自视觉枪口时，记录导致回退的失败原因。
            /// </summary>
            public string RootOriginFailureKind { get; set; }

            /// <summary>
            /// 本发 projectile 记录时命中的视觉投影版本号。
            /// </summary>
            public int ProjectionVersion { get; set; }

            /// <summary>
            /// 本发 projectile 记录时命中的姿态样本 tick。
            /// </summary>
            public int PoseSampleTick { get; set; }
        }
    }

    /// <summary>
    /// 最近一次正式发射批次的只读诊断快照。
    /// 主模组和 DevHarness 只交换这份 DTO，不共享可变运行时状态。
    /// </summary>
    public sealed class TriggerVisualEmissionDiagnosticsSnapshot
    {
        /// <summary>
        /// 当前是否存在可用的最近发射批次记录。
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 最近发射批次所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 最近发射批次发生时的游戏 tick。
        /// </summary>
        public int LaunchTick { get; set; }

        /// <summary>
        /// 最近发射批次包含的全部发射点。
        /// </summary>
        public List<TriggerVisualEmissionLaunchPointSnapshot> LaunchPoints { get; set; } =
            new List<TriggerVisualEmissionLaunchPointSnapshot>();
    }

    /// <summary>
    /// 单发 projectile 的只读发射点快照。
    /// 它把理论中心原点与真实发射原点成对暴露给 DevHarness overlay。
    /// </summary>
    public sealed class TriggerVisualEmissionLaunchPointSnapshot
    {
        /// <summary>
        /// 本发 projectile 所属的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 本发 projectile 当前采用的根原点。
        /// 理论中心与真实发射点都应在它的基础上继续偏移。
        /// </summary>
        public Vector3 RootOriginWorld { get; set; }

        /// <summary>
        /// 本发 projectile 的理论中心原点。
        /// 不考虑起点散布时，所有同批次同武器的子弹应共享这个中心点。
        /// </summary>
        public Vector3 TheoreticalCenterOriginWorld { get; set; }

        /// <summary>
        /// 本发 projectile 最终实际使用的真实发射原点。
        /// 有起点散布时，它会围绕 TheoreticalCenterOriginWorld 分布。
        /// </summary>
        public Vector3 ActualLaunchOriginWorld { get; set; }

        /// <summary>
        /// 本发 projectile 计划声明的世界偏移量。
        /// </summary>
        public Vector3 OriginOffsetWorld { get; set; }

        /// <summary>
        /// 本发 projectile 是否直接使用绝对世界原点作为理论中心。
        /// </summary>
        public bool UsesAbsoluteOriginWorld { get; set; }

        /// <summary>
        /// 本发 projectile 实际采用的根原点来源类型。
        /// </summary>
        public string RootOriginSourceKind { get; set; }

        /// <summary>
        /// 当根原点没有来自视觉枪口时，记录导致回退的失败原因。
        /// </summary>
        public string RootOriginFailureKind { get; set; }

        /// <summary>
        /// 本发 projectile 所属批次发生时的游戏 tick。
        /// </summary>
        public int LaunchTick { get; set; }

        /// <summary>
        /// 本发 projectile 记录时命中的视觉投影版本号。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 本发 projectile 记录时命中的姿态样本 tick。
        /// </summary>
        public int PoseSampleTick { get; set; }
    }
}
