using System.Collections.Generic;
using BDP.Content.RangedModules.OrderedSpread;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.DistanceRevector
{
    /// <summary>
    /// 按飞行距离阶段重定向投射物的配置。
    /// </summary>
    public sealed class DistanceRevectorConfig : RangedModuleConfigNode, IExposable
    {
        /// <summary>
        /// 默认距离检查点；最后自动补入 1，保证末段回到原版正常命中流程。
        /// </summary>
        public List<float> CheckpointRatios = new List<float>
        {
            0.20f,
            0.45f,
            0.70f,
            0.85f,
            0.93f,
            0.97f
        };

        /// <summary>中间距离检查点相对名义点的最大散布半径。</summary>
        public float IntermediateSpreadRadius = 0.625f;

        /// <summary>最终距离检查点相对目标格中心的最大散布半径。</summary>
        public float FinalSpreadRadius = 0.30f;

        /// <summary>原版精度达到最高时仍保留的散布比例。</summary>
        public float HighAccuracySpreadScale = 0.25f;

        /// <summary>
        /// 是否按发射顺序使用有序横向车道。
        /// 默认关闭，避免改变其它 DistanceRevector 使用者的既有散布行为。
        /// </summary>
        public bool UseOrderedParallelLanes;

        /// <summary>
        /// 生成独立的配置快照。
        /// </summary>
        public override RangedModuleConfigNode Clone()
        {
            DistanceRevectorConfig clone = new DistanceRevectorConfig();
            clone.CheckpointRatios = CheckpointRatios != null
                ? new List<float>(CheckpointRatios)
                : new List<float>();
            clone.IntermediateSpreadRadius = IntermediateSpreadRadius;
            clone.FinalSpreadRadius = FinalSpreadRadius;
            clone.HighAccuracySpreadScale = HighAccuracySpreadScale;
            clone.UseOrderedParallelLanes = UseOrderedParallelLanes;
            return clone;
        }

        /// <summary>
        /// 持久化距离检查点配置。
        /// </summary>
        public void ExposeData()
        {
            List<float> checkpointRatios = CheckpointRatios;
            float intermediateSpreadRadius = IntermediateSpreadRadius;
            float finalSpreadRadius = FinalSpreadRadius;
            float highAccuracySpreadScale = HighAccuracySpreadScale;
            bool useOrderedParallelLanes = UseOrderedParallelLanes;
            Scribe_Collections.Look(ref checkpointRatios, "checkpointRatios", LookMode.Value);
            Scribe_Values.Look(ref intermediateSpreadRadius, "intermediateSpreadRadius", 0.625f);
            Scribe_Values.Look(ref finalSpreadRadius, "finalSpreadRadius", 0.30f);
            Scribe_Values.Look(ref highAccuracySpreadScale, "highAccuracySpreadScale", 0.25f);
            Scribe_Values.Look(ref useOrderedParallelLanes, "useOrderedParallelLanes", false);
            CheckpointRatios = checkpointRatios ?? new List<float>();
            IntermediateSpreadRadius = intermediateSpreadRadius;
            FinalSpreadRadius = finalSpreadRadius;
            HighAccuracySpreadScale = highAccuracySpreadScale;
            UseOrderedParallelLanes = useOrderedParallelLanes;
        }
    }

    /// <summary>
    /// 每枚投射物独立的距离重定向状态。
    /// </summary>
    public sealed class DistanceRevectorState : IRangedModulePrivateContext
    {
        /// <summary>当前攻击冻结的配置。</summary>
        public DistanceRevectorConfig FrozenConfig { get; set; } = new DistanceRevectorConfig();

        /// <summary>每枚投射物的独立轨迹推进状态。</summary>
        public List<DistanceRevectorTrack> Tracks { get; set; } = new List<DistanceRevectorTrack>();

        /// <summary>
        /// 查找或创建某枚投射物的轨迹状态。
        /// </summary>
        public DistanceRevectorTrack GetOrCreateTrack(
            int emitSequence,
            LocalTargetInfo target,
            Vector3 start,
            IntVec3 targetCell,
            IntVec3 targetOriginCell)
        {
            Tracks = Tracks ?? new List<DistanceRevectorTrack>();
            for (int index = 0; index < Tracks.Count; index++)
            {
                if (Tracks[index] != null && Tracks[index].EmitSequence == emitSequence)
                {
                    return Tracks[index];
                }
            }

            DistanceRevectorTrack created = new DistanceRevectorTrack
            {
                EmitSequence = emitSequence,
                LockedTarget = target,
                Start = start,
                TargetCell = targetCell,
                HasTargetCellOffset = target.HasThing && targetOriginCell.IsValid && targetCell.IsValid,
                TargetCellOffset = targetCell.IsValid && targetOriginCell.IsValid
                    ? targetCell - targetOriginCell
                    : IntVec3.Zero,
                NextCheckpointIndex = 1
            };
            Tracks.Add(created);
            return created;
        }

        /// <summary>
        /// 读取某枚投射物的轨迹状态。
        /// </summary>
        public DistanceRevectorTrack GetTrack(int emitSequence)
        {
            if (Tracks == null)
            {
                return null;
            }

            for (int index = 0; index < Tracks.Count; index++)
            {
                if (Tracks[index] != null && Tracks[index].EmitSequence == emitSequence)
                {
                    return Tracks[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 深复制模块私有状态。
        /// </summary>
        public IAttackContextNode Clone()
        {
            DistanceRevectorState clone = new DistanceRevectorState
            {
                FrozenConfig = FrozenConfig != null
                    ? FrozenConfig.Clone() as DistanceRevectorConfig
                    : new DistanceRevectorConfig(),
                Tracks = new List<DistanceRevectorTrack>()
            };
            if (Tracks != null)
            {
                for (int index = 0; index < Tracks.Count; index++)
                {
                    if (Tracks[index] != null)
                    {
                        clone.Tracks.Add(Tracks[index].CloneTyped());
                    }
                }
            }

            return clone;
        }

        /// <summary>
        /// 持久化模块私有状态。
        /// </summary>
        public void ExposeData()
        {
            DistanceRevectorConfig frozenConfig = FrozenConfig;
            List<DistanceRevectorTrack> tracks = Tracks;
            Scribe_Deep.Look(ref frozenConfig, "frozenConfig");
            Scribe_Collections.Look(ref tracks, "tracks", LookMode.Deep);
            FrozenConfig = frozenConfig ?? new DistanceRevectorConfig();
            Tracks = tracks ?? new List<DistanceRevectorTrack>();
        }
    }

    /// <summary>
    /// 单枚投射物的距离重定向轨迹状态。
    /// </summary>
    public sealed class DistanceRevectorTrack : IExposable
    {
        /// <summary>对应的轮内发射序号。</summary>
        public int EmitSequence { get; set; }

        /// <summary>首段真实飞行起点。</summary>
        public Vector3 Start { get; set; }

        /// <summary>冻结的目标实体与选中目标格。</summary>
        public LocalTargetInfo LockedTarget { get; set; } = LocalTargetInfo.Invalid;

        /// <summary>发射时选中的目标格。</summary>
        public IntVec3 TargetCell { get; set; } = IntVec3.Invalid;

        /// <summary>目标格是否相对目标实体位置保存。</summary>
        public bool HasTargetCellOffset { get; set; }

        /// <summary>选中目标格相对目标实体根格的偏移。</summary>
        public IntVec3 TargetCellOffset { get; set; } = IntVec3.Zero;

        /// <summary>下一次要使用的检查点下标。</summary>
        public int NextCheckpointIndex { get; set; }

        /// <summary>是否冻结了当前投射物的有序横向车道。</summary>
        public bool HasParallelLane { get; set; }

        /// <summary>当前投射物的归一化横向车道值，范围为 -1 到 1。</summary>
        public float ParallelLaneUnit { get; set; }

        /// <summary>发射时冻结的右侧横向方向。</summary>
        public Vector3 ParallelLaneDirection { get; set; } = Vector3.right;

        /// <summary>复制当前投射物轨迹状态。</summary>
        public DistanceRevectorTrack CloneTyped()
        {
            return new DistanceRevectorTrack
            {
                EmitSequence = EmitSequence,
                Start = Start,
                LockedTarget = LockedTarget,
                TargetCell = TargetCell,
                HasTargetCellOffset = HasTargetCellOffset,
                TargetCellOffset = TargetCellOffset,
                NextCheckpointIndex = NextCheckpointIndex,
                HasParallelLane = HasParallelLane,
                ParallelLaneUnit = ParallelLaneUnit,
                ParallelLaneDirection = ParallelLaneDirection
            };
        }

        /// <summary>持久化当前投射物轨迹状态。</summary>
        public void ExposeData()
        {
            int emitSequence = EmitSequence;
            Vector3 start = Start;
            LocalTargetInfo lockedTarget = LockedTarget;
            IntVec3 targetCell = TargetCell;
            bool hasTargetCellOffset = HasTargetCellOffset;
            IntVec3 targetCellOffset = TargetCellOffset;
            int nextCheckpointIndex = NextCheckpointIndex;
            bool hasParallelLane = HasParallelLane;
            float parallelLaneUnit = ParallelLaneUnit;
            Vector3 parallelLaneDirection = ParallelLaneDirection;
            Scribe_Values.Look(ref emitSequence, "emitSequence", 0);
            Scribe_Values.Look(ref start, "start");
            Scribe_TargetInfo.Look(ref lockedTarget, "lockedTarget");
            Scribe_Values.Look(ref targetCell, "targetCell", IntVec3.Invalid);
            Scribe_Values.Look(ref hasTargetCellOffset, "hasTargetCellOffset", false);
            Scribe_Values.Look(ref targetCellOffset, "targetCellOffset", IntVec3.Zero);
            Scribe_Values.Look(ref nextCheckpointIndex, "nextCheckpointIndex", 1);
            Scribe_Values.Look(ref hasParallelLane, "hasParallelLane", false);
            Scribe_Values.Look(ref parallelLaneUnit, "parallelLaneUnit", 0f);
            Scribe_Values.Look(ref parallelLaneDirection, "parallelLaneDirection", Vector3.right);
            EmitSequence = emitSequence;
            Start = start;
            LockedTarget = lockedTarget;
            TargetCell = targetCell;
            HasTargetCellOffset = hasTargetCellOffset;
            TargetCellOffset = targetCellOffset;
            // 0 是有序车道的新首个外张检查点；旧轨迹缺少该字段时仍由 Scribe 默认值保持从 1 开始。
            NextCheckpointIndex = nextCheckpointIndex < 0 ? 0 : nextCheckpointIndex;
            HasParallelLane = hasParallelLane;
            ParallelLaneUnit = Mathf.Clamp(parallelLaneUnit, -1f, 1f);
            ParallelLaneDirection = parallelLaneDirection.sqrMagnitude > 0.0001f
                ? parallelLaneDirection.normalized
                : Vector3.right;
        }
    }

    /// <summary>
    /// 按距离检查点把投射物重定向到实时目标的模块。
    /// </summary>
    public sealed class DistanceRevectorModule :
        IRangedAttackModuleRuntime,
        IProjectileInitStageModule,
        IArrivalStageModule
    {
        /// <summary>
        /// 有序车道首段允许使用的最早安全进度。
        /// BDP 投射物宿主会将初始段进度限制在不低于 1%，用于让有序车道从发射后立即进入外张阶段。
        /// </summary>
        private const float OrderedLaneExpansionStartRatio = 0.01f;

        /// <summary>
        /// 有序车道在目标处保留的最小横向车道比例，避免所有轨迹收束为同一个点。
        /// </summary>
        private const float OrderedLaneTerminalEnvelope = 0.60f;

        /// <summary>当前模块的配置快照。</summary>
        private DistanceRevectorConfig config;

        /// <summary>初始化模块运行时。</summary>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = context?.Config is DistanceRevectorConfig typedConfig
                ? typedConfig.Clone() as DistanceRevectorConfig
                : new DistanceRevectorConfig();
        }

        /// <summary>
        /// 为每枚投射物准备目标格、首段完整几何和早期距离检查点。
        /// </summary>
        void IProjectileInitStageModule.Contribute(
            in ProjectileInitStageContext context,
            ProjectileInitContribution contribution)
        {
            if (contribution == null || context.Pawn == null)
            {
                return;
            }

            DistanceRevectorConfig frozenConfig = NormalizeConfig(config);
            DistanceRevectorState state = context.GetOrCreatePrivateContext<DistanceRevectorState>();
            if (state == null)
            {
                return;
            }

            state.FrozenConfig = frozenConfig;
            LocalTargetInfo target = context.FinalTarget.IsValid
                ? context.FinalTarget
                : context.RequestedTarget;
            if (!target.IsValid)
            {
                return;
            }

            Vector3 start = context.Pawn.DrawPos.Yto0();
            IntVec3 selectedCell = target.Cell.IsValid
                ? target.Cell
                : target.HasThing ? target.Thing.Position : IntVec3.Invalid;
            IntVec3 targetOriginCell = target.HasThing && target.Thing != null
                ? target.Thing.Position
                : selectedCell;
            float firstCheckpoint = frozenConfig.CheckpointRatios[0];
            Vector3 targetPoint = ResolveTargetPoint(target, selectedCell);
            Vector3 initialDirection = (targetPoint - start).normalized;
            Vector3 parallelLaneDirection = ResolveParallelLaneDirection(initialDirection);
            float sharedForwardSpread = frozenConfig.UseOrderedParallelLanes && context.EmitCount > 1
                ? ResolveSharedForwardSpreadOffset(context)
                : 0f;
            for (int emitIndex = 0; emitIndex < context.EmitCount; emitIndex++)
            {
                int emitSequence = context.EmitSequenceBase + emitIndex;
                bool useParallelLane = frozenConfig.UseOrderedParallelLanes;
                string sourceResultId;
                if (!context.TryGetEmitSourceResultId(emitIndex, out sourceResultId))
                {
                    sourceResultId = context.AttackInstanceId;
                }

                float parallelLaneUnit = useParallelLane
                    ? ResolveParallelLane(
                        context.AttackInstanceId,
                        sourceResultId,
                        emitIndex,
                        context.EmitCount)
                    : 0f;
                bool beginParallelLaneExpansionImmediately = useParallelLane && context.EmitCount > 1;
                // 有序车道统一从同一横向轴开始，避免原版横向发射散布先把轨迹放宽、
                // 首段又拉回中心后再重新外张；前向微偏移仍按齐射共享，保持发射层轻微错位。
                Vector3 originSpreadOffset = beginParallelLaneExpansionImmediately
                    ? initialDirection * sharedForwardSpread
                    : Vector3.zero;
                bool hasOriginSpreadOverride = beginParallelLaneExpansionImmediately;

                Vector3 emitStart = start + originSpreadOffset;
                DistanceRevectorTrack emitTrack = state.GetOrCreateTrack(
                    emitSequence,
                    target,
                    emitStart,
                    selectedCell,
                    targetOriginCell);
                emitTrack.Start = emitStart;
                emitTrack.LockedTarget = target;
                emitTrack.TargetCell = selectedCell;
                emitTrack.NextCheckpointIndex = beginParallelLaneExpansionImmediately ? 0 : 1;
                emitTrack.HasParallelLane = useParallelLane;
                emitTrack.ParallelLaneUnit = parallelLaneUnit;
                emitTrack.ParallelLaneDirection = parallelLaneDirection;

                ProjectileInitPlanContribution planContribution = new ProjectileInitPlanContribution
                {
                    EmitIndex = emitIndex,
                    HasOverrideCurrentTarget = true,
                    OverrideCurrentTarget = target,
                    HasInitialSegmentTriggerRatio = true,
                    InitialSegmentTriggerRatio = beginParallelLaneExpansionImmediately
                        ? OrderedLaneExpansionStartRatio
                        : firstCheckpoint,
                    HasInitialFlightPathSnapshot = true,
                    InitialFlightPathSnapshot = ProjectileFlightPathUtility.CreateLinear(emitStart, targetPoint),
                    HasInteractionPolicy = true,
                    InteractionPolicy = new ProjectileInteractionPolicy
                    {
                        // 原版 flags 适配与 BDP 自有扫描共用同一份非目标放行判定。
                        AllowNonTargetPassThrough = true
                    }
                };
                if (hasOriginSpreadOverride)
                {
                    planContribution.HasOriginSpreadOffsetOverride = true;
                    planContribution.OriginSpreadOffsetOverrideWorld = originSpreadOffset;
                }

                contribution.PlanContributions.Add(planContribution);
            }
        }

        /// <summary>
        /// 在每个距离检查点创建下一条直线段；最后一个检查点之后停止接管，回到原版命中。
        /// </summary>
        void IArrivalStageModule.Contribute(
            in ArrivalStageContext context,
            ArrivalContribution contribution)
        {
            if (contribution == null || context.Projectile == null)
            {
                return;
            }

            DistanceRevectorState state = context.GetPrivateContext<DistanceRevectorState>();
            DistanceRevectorTrack track = state?.GetTrack(context.EmitIndex);
            DistanceRevectorConfig frozenConfig = state?.FrozenConfig;
            if (track == null || frozenConfig?.CheckpointRatios == null)
            {
                return;
            }

            if (track.NextCheckpointIndex >= frozenConfig.CheckpointRatios.Count)
            {
                contribution.HasOverrideContinueFlight = true;
                contribution.OverrideContinueFlight = false;
                return;
            }

            LocalTargetInfo liveTarget = ResolveLiveTarget(track, context.Map);
            IntVec3 liveTargetCell = ResolveLiveTargetCell(track, liveTarget, context.Map);
            if (!liveTargetCell.IsValid || context.Map == null || !liveTargetCell.InBounds(context.Map))
            {
                contribution.HasOverrideContinueFlight = true;
                contribution.OverrideContinueFlight = false;
                return;
            }

            float ratio = frozenConfig.CheckpointRatios[track.NextCheckpointIndex];
            int checkpointIndex = track.NextCheckpointIndex;
            bool isFinal = checkpointIndex == frozenConfig.CheckpointRatios.Count - 1;
            track.NextCheckpointIndex++;
            Vector3 start = context.Projectile.ExactPosition.Yto0();
            Vector3 targetPoint = liveTargetCell.ToVector3Shifted();
            Vector3 nominalPoint = Vector3.Lerp(track.Start, targetPoint, ratio);
            float spreadRadius = OrderedSpreadSampler.ResolveScaledRadius(
                context.AccuracySnapshot,
                isFinal ? frozenConfig.FinalSpreadRadius : frozenConfig.IntermediateSpreadRadius,
                frozenConfig.HighAccuracySpreadScale);
            Vector3 nextPoint;
            if (track.HasParallelLane)
            {
                Vector3 laneDirection = track.ParallelLaneDirection.sqrMagnitude > 0.0001f
                    ? track.ParallelLaneDirection.normalized
                    : Vector3.right;
                nextPoint = ResolveParallelLanePoint(
                    track.Start,
                    targetPoint,
                    ratio,
                    laneDirection,
                    track.ParallelLaneUnit,
                    spreadRadius);
            }
            else
            {
                nextPoint = nominalPoint + OrderedSpreadSampler.SampleOffset(
                    spreadRadius,
                    context.AttackInstanceId,
                    context.ResultId,
                    track.EmitSequence,
                    checkpointIndex,
                    isFinal
                        ? OrderedSpreadSampler.SampleKind.Final
                        : OrderedSpreadSampler.SampleKind.Intermediate);
            }
            ProjectileFlightPathSnapshot nextSnapshot = ProjectileFlightPathUtility.CreateLinear(start, nextPoint);

            contribution.HasOverrideContinueFlight = true;
            contribution.OverrideContinueFlight = true;
            contribution.HasNextTarget = true;
            contribution.NextTarget = liveTarget;
            contribution.HasNextBindingTarget = true;
            contribution.NextBindingTarget = liveTarget;
            contribution.HasNextDestination = true;
            contribution.NextDestination = nextPoint;
            contribution.HasNextFlightPathSnapshot = true;
            contribution.NextFlightPathSnapshot = nextSnapshot;
        }

        /// <summary>
        /// 规范化配置：排序、去重、限制到有效距离，并确保存在终点 1。
        /// </summary>
        private static DistanceRevectorConfig NormalizeConfig(DistanceRevectorConfig source)
        {
            DistanceRevectorConfig normalized = source != null
                ? source.Clone() as DistanceRevectorConfig
                : new DistanceRevectorConfig();
            List<float> values = new List<float>();
            if (normalized.CheckpointRatios != null)
            {
                for (int index = 0; index < normalized.CheckpointRatios.Count; index++)
                {
                    float configuredRatio = normalized.CheckpointRatios[index];
                    if (float.IsNaN(configuredRatio) || float.IsInfinity(configuredRatio))
                    {
                        continue;
                    }

                    float ratio = Mathf.Clamp01(configuredRatio);
                    if (ratio < 0.01f || ratio >= 0.9999f)
                    {
                        continue;
                    }

                    if (!values.Contains(ratio))
                    {
                        values.Add(ratio);
                    }
                }
            }

            values.Sort();
            if (values.Count == 0)
            {
                values.Add(0.05f);
            }

            values.Add(1f);
            normalized.CheckpointRatios = values;
            normalized.IntermediateSpreadRadius = Mathf.Max(0f, normalized.IntermediateSpreadRadius);
            normalized.FinalSpreadRadius = Mathf.Max(0f, normalized.FinalSpreadRadius);
            normalized.HighAccuracySpreadScale = Mathf.Clamp01(normalized.HighAccuracySpreadScale);
            return normalized;
        }

        /// <summary>
        /// 按齐射顺序分配稳定横向车道；扰动小于相邻车道间距的一半，因此不会交换顺序。
        /// </summary>
        private static float ResolveParallelLane(
            string attackInstanceId,
            string resultId,
            int emitIndex,
            int emitCount)
        {
            int safeEmitCount = emitCount > 0 ? emitCount : 1;
            if (safeEmitCount <= 1)
            {
                return 0f;
            }

            int rank = Mathf.Clamp(emitIndex, 0, safeEmitCount - 1);
            float spacing = 2f / safeEmitCount;
            float lane = -1f + (rank + 0.5f) * spacing;
            int seed = BuildParallelSpreadSeed(attackInstanceId, resultId, emitIndex, 7919);
            Rand.PushState(seed);
            try
            {
                float jitter = Rand.Range(-spacing * 0.18f, spacing * 0.18f);
                return Mathf.Clamp(lane + jitter, -1f, 1f);
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>
        /// 为当前齐射所有发次解析同一个横向右侧方向，避免目标移动导致车道翻转。
        /// </summary>
        private static Vector3 ResolveParallelLaneDirection(Vector3 forward)
        {
            Vector3 safeForward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, safeForward).normalized;
            return right.sqrMagnitude > 0.0001f ? right : Vector3.right;
        }

        /// <summary>
        /// <summary>
        /// 在齐射范围内只取一次既有前后散布，保证所有发次起点保持同一车道关系。
        /// </summary>
        private static float ResolveSharedForwardSpreadOffset(in ProjectileInitStageContext context)
        {
            for (int emitIndex = 0; emitIndex < context.EmitCount; emitIndex++)
            {
                if (!context.TryGetEmitOriginSpread(
                        emitIndex,
                        out float lateralMin,
                        out float lateralMax,
                        out float forwardMin,
                        out float forwardMax))
                {
                    continue;
                }

                string resultId;
                if (!context.TryGetEmitSourceResultId(emitIndex, out resultId))
                {
                    resultId = context.AttackInstanceId;
                }

                float lower = Mathf.Min(forwardMin, forwardMax);
                float upper = Mathf.Max(forwardMin, forwardMax);
                int seed = BuildParallelSpreadSeed(
                    context.AttackInstanceId,
                    resultId,
                    context.EmitSequenceBase,
                    15401);
                Rand.PushState(seed);
                try
                {
                    return Rand.Range(lower, upper);
                }
                finally
                {
                    Rand.PopState();
                }
            }

            return 0f;
        }

        /// <summary>
        /// 让车道在前中段保持不变，并在最后 15% 距离向目标收束但保留轻微横向间距。
        /// </summary>
        private static float ResolveParallelLaneEnvelope(float ratio)
        {
            float safeRatio = Mathf.Clamp01(ratio);
            float envelope = safeRatio <= 0.85f
                ? 1f
                : 1f - Mathf.InverseLerp(0.85f, 1f, safeRatio);
            return Mathf.Max(OrderedLaneTerminalEnvelope, envelope);
        }

        /// <summary>
        /// 解析有序车道在指定距离检查点的目标位置。
        /// </summary>
        private static Vector3 ResolveParallelLanePoint(
            Vector3 start,
            Vector3 targetPoint,
            float ratio,
            Vector3 laneDirection,
            float laneUnit,
            float spreadRadius)
        {
            Vector3 nominalPoint = Vector3.Lerp(start, targetPoint, Mathf.Clamp01(ratio));
            Vector3 safeLaneDirection = laneDirection.sqrMagnitude > 0.0001f
                ? laneDirection.normalized
                : Vector3.right;
            return nominalPoint + safeLaneDirection
                * laneUnit
                * Mathf.Max(0f, spreadRadius)
                * ResolveParallelLaneEnvelope(ratio);
        }

        /// <summary>
        /// 生成不依赖全局随机状态的有序车道样本种子。
        /// </summary>
        private static int BuildParallelSpreadSeed(
            string attackInstanceId,
            string resultId,
            int emitIndex,
            int channel)
        {
            int seed = 203917;
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(attackInstanceId)
                ? GenText.StableStringHash(attackInstanceId)
                : 0);
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(resultId)
                ? GenText.StableStringHash(resultId)
                : 0);
            seed = Gen.HashCombineInt(seed, emitIndex);
            seed = Gen.HashCombineInt(seed, channel);
            return seed;
        }

        /// <summary>
        /// 优先使用实时目标的选中格，并保持多格目标相对根格的偏移。
        /// </summary>
        private static IntVec3 ResolveLiveTargetCell(
            DistanceRevectorTrack track,
            LocalTargetInfo target,
            Map map)
        {
            if (track == null)
            {
                return IntVec3.Invalid;
            }

            if (target.HasThing && target.Thing != null && target.Thing.Spawned && track.HasTargetCellOffset)
            {
                IntVec3 movedCell = target.Thing.Position + track.TargetCellOffset;
                if (map == null || movedCell.InBounds(map))
                {
                    return movedCell;
                }
            }

            return track.TargetCell.IsValid
                ? track.TargetCell
                : target.Cell;
        }

        /// <summary>
        /// 目标实体仍在场时继续追踪实体；实体消失后退回最后冻结的目标格。
        /// </summary>
        private static LocalTargetInfo ResolveLiveTarget(
            DistanceRevectorTrack track,
            Map map)
        {
            if (track != null && track.LockedTarget.HasThing && track.LockedTarget.Thing != null
                && track.LockedTarget.Thing.Spawned && track.LockedTarget.Thing.Map == map)
            {
                return track.LockedTarget;
            }

            IntVec3 fallbackCell = track != null ? track.TargetCell : IntVec3.Invalid;
            return fallbackCell.IsValid ? new LocalTargetInfo(fallbackCell) : LocalTargetInfo.Invalid;
        }

        /// <summary>
        /// 解析首段选中目标格的几何中心。
        /// </summary>
        private static Vector3 ResolveTargetPoint(LocalTargetInfo target, IntVec3 selectedCell)
        {
            return selectedCell.IsValid ? selectedCell.ToVector3Shifted() : target.CenterVector3;
        }
    }
}
