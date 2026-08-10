using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.PathInput;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.RoutePath
{
    /// <summary>
    /// 路线引导模块在确认后冻结的路径来源。
    /// </summary>
    public enum RoutePathSource
    {
        /// <summary>没有中间锚点，直接射向最终目标。</summary>
        Direct,

        /// <summary>路径锚点来自玩家手动输入。</summary>
        Manual,

        /// <summary>路径锚点来自自动绕障解析。</summary>
        Auto
    }

    /// <summary>
    /// 路线引导模块私有状态根节点。
    /// </summary>
    public sealed class RoutePathState : IRangedModulePrivateContext
    {
        /// <summary>当前目标选择期的临时输入状态。</summary>
        public RouteInputState InputState { get; set; } = new RouteInputState();

        /// <summary>当前确认冻结后的路径快照。</summary>
        public RouteConfirmedSnapshot ConfirmedSnapshot { get; set; } = new RouteConfirmedSnapshot();

        /// <summary>当前投射物阶段使用的路径上下文。</summary>
        public RoutePathContext PathSnapshot { get; set; } = new RoutePathContext();

        public IAttackContextNode Clone()
        {
            return new RoutePathState
            {
                InputState = InputState != null ? InputState.CloneRouteTyped() : new RouteInputState(),
                ConfirmedSnapshot = ConfirmedSnapshot != null ? ConfirmedSnapshot.CloneTyped() : new RouteConfirmedSnapshot(),
                PathSnapshot = PathSnapshot != null ? PathSnapshot.CloneTyped() : new RoutePathContext()
            };
        }

        public void ExposeData()
        {
            RouteInputState inputState = InputState;
            RouteConfirmedSnapshot confirmedSnapshot = ConfirmedSnapshot;
            RoutePathContext pathSnapshot = PathSnapshot;

            Scribe_Deep.Look(ref inputState, "inputState");
            Scribe_Deep.Look(ref confirmedSnapshot, "confirmedSnapshot");
            Scribe_Deep.Look(ref pathSnapshot, "pathSnapshot");

            InputState = inputState ?? new RouteInputState();
            ConfirmedSnapshot = confirmedSnapshot ?? new RouteConfirmedSnapshot();
            PathSnapshot = pathSnapshot ?? new RoutePathContext();
        }
    }

    /// <summary>
    /// <para>【已迁移】单个路径锚点类型已迁移至 BDP.Core.PathInput.PathAnchor。</para>
    /// <para>RouteAnchor 保留为别名以兼容旧存档 Scribe 类型名，内部完全委托给 PathAnchor。</para>
    /// </summary>
    public sealed class RouteAnchor : PathAnchor { }

    /// <summary>
    /// <para>【已迁移】RouteInputState 现在继承自 BDP.Core.PathInput.PathInputState。</para>
    /// <para>所有数据由基类提供，此为 RoutePath 模块的类型别名，保持引用兼容。</para>
    /// </summary>
    public sealed class RouteInputState : PathInputState
    {
        /// <summary>从基类深复制数据到当前类型的新实例。</summary>
        public RouteInputState CloneRouteTyped()
        {
            RouteInputState clone = new RouteInputState();
            PathInputState baseClone = CloneTyped();
            clone.Anchors = baseClone.Anchors;
            clone.HasFinalTarget = baseClone.HasFinalTarget;
            clone.FinalTarget = baseClone.FinalTarget;
            clone.FinalIsThing = baseClone.FinalIsThing;
            return clone;
        }
    }

    /// <summary>
    /// 确认阶段冻结下来的路径快照。
    /// </summary>
    public sealed class RouteConfirmedSnapshot : IExposable
    {
        /// <summary>当前确认后保留的锚点列表。</summary>
        public List<PathAnchor> Anchors { get; set; } = new List<PathAnchor>();

        /// <summary>自动路径左侧候选锚点。</summary>
        public List<PathAnchor> AutoLeftAnchors { get; set; } = new List<PathAnchor>();

        /// <summary>自动路径右侧候选锚点。</summary>
        public List<PathAnchor> AutoRightAnchors { get; set; } = new List<PathAnchor>();

        /// <summary>当前是否存在最终目标。</summary>
        public bool HasFinalTarget { get; set; }

        /// <summary>当前冻结下来的最终目标。</summary>
        public LocalTargetInfo FinalTarget { get; set; }

        /// <summary>当前最终目标是否是 Thing。</summary>
        public bool FinalIsThing { get; set; }

        /// <summary>当前确认路径的来源。</summary>
        public RoutePathSource PathSource { get; set; }

        /// <summary>清空当前确认快照。</summary>
        public void Reset()
        {
            Anchors = Anchors ?? new List<PathAnchor>();
            AutoLeftAnchors = AutoLeftAnchors ?? new List<PathAnchor>();
            AutoRightAnchors = AutoRightAnchors ?? new List<PathAnchor>();
            Anchors.Clear();
            AutoLeftAnchors.Clear();
            AutoRightAnchors.Clear();
            HasFinalTarget = false;
            FinalTarget = LocalTargetInfo.Invalid;
            FinalIsThing = false;
            PathSource = RoutePathSource.Direct;
        }

        /// <summary>复制当前确认快照。</summary>
        public RouteConfirmedSnapshot CloneTyped()
        {
            RouteConfirmedSnapshot clone = new RouteConfirmedSnapshot
            {
                HasFinalTarget = HasFinalTarget,
                FinalTarget = FinalTarget,
                FinalIsThing = FinalIsThing,
                PathSource = PathSource
            };

            for (int i = 0; i < Anchors.Count; i++)
            {
                if (Anchors[i] != null) clone.Anchors.Add(Anchors[i].CloneTyped());
            }

            for (int i = 0; AutoLeftAnchors != null && i < AutoLeftAnchors.Count; i++)
            {
                if (AutoLeftAnchors[i] != null) clone.AutoLeftAnchors.Add(AutoLeftAnchors[i].CloneTyped());
            }

            for (int i = 0; AutoRightAnchors != null && i < AutoRightAnchors.Count; i++)
            {
                if (AutoRightAnchors[i] != null) clone.AutoRightAnchors.Add(AutoRightAnchors[i].CloneTyped());
            }

            return clone;
        }

        public void ExposeData()
        {
            List<PathAnchor> anchors = Anchors;
            List<PathAnchor> autoLeftAnchors = AutoLeftAnchors;
            List<PathAnchor> autoRightAnchors = AutoRightAnchors;
            bool hasFinalTarget = HasFinalTarget;
            LocalTargetInfo finalTarget = FinalTarget;
            bool finalIsThing = FinalIsThing;
            RoutePathSource pathSource = PathSource;

            Scribe_Collections.Look(ref anchors, "anchors", LookMode.Deep);
            Scribe_Collections.Look(ref autoLeftAnchors, "autoLeftAnchors", LookMode.Deep);
            Scribe_Collections.Look(ref autoRightAnchors, "autoRightAnchors", LookMode.Deep);
            Scribe_Values.Look(ref hasFinalTarget, "hasFinalTarget", false);
            Scribe_TargetInfo.Look(ref finalTarget, "finalTarget");
            Scribe_Values.Look(ref finalIsThing, "finalIsThing", false);
            Scribe_Values.Look(ref pathSource, "pathSource", RoutePathSource.Direct);

            Anchors = anchors ?? new List<PathAnchor>();
            AutoLeftAnchors = autoLeftAnchors ?? new List<PathAnchor>();
            AutoRightAnchors = autoRightAnchors ?? new List<PathAnchor>();
            HasFinalTarget = hasFinalTarget;
            FinalTarget = finalTarget;
            FinalIsThing = finalIsThing;
            PathSource = pathSource;
        }
    }

    /// <summary>
    /// 投射物阶段使用的路径上下文。
    /// </summary>
    public sealed class RoutePathContext : IExposable
    {
        /// <summary>当前投射物阶段保留的锚点列表。</summary>
        public List<PathAnchor> Anchors { get; set; } = new List<PathAnchor>();

        /// <summary>当前是否存在最终目标。</summary>
        public bool HasFinalTarget { get; set; }

        /// <summary>当前冻结下来的最终目标。</summary>
        public LocalTargetInfo FinalTarget { get; set; }

        /// <summary>当前最终目标是否是 Thing。</summary>
        public bool FinalIsThing { get; set; }

        /// <summary>当前投射物路径的来源。</summary>
        public RoutePathSource PathSource { get; set; }

        /// <summary>当前投射物正在飞向第几段正式目标。0 表示首段。</summary>
        public int CurrentLegIndex { get; set; }

        /// <summary>已按哪个 emit 序号完成自动路径分配。-1 表示未分配。</summary>
        public int AssignedEmitIndex { get; set; } = -1;

        /// <summary>当前段末匹配使用的容差半径。</summary>
        public float ArrivalTolerance { get; set; } = 0.35f;

        /// <summary>当前投射物中间续段使用的最大散布半径。</summary>
        public float IntermediateSpreadRadius { get; set; } = 0.625f;

        /// <summary>当前投射物最终续段使用的最大散布半径。</summary>
        public float FinalSpreadRadius { get; set; } = 0.30f;

        /// <summary>原版精度达到最高时仍保留的散布比例。</summary>
        public float HighAccuracySpreadScale { get; set; } = 0.25f;

        /// <summary>候选散布不安全时允许折半收缩的次数。</summary>
        public int SpreadSafetyShrinkSteps { get; set; } = 4;

        /// <summary>是否已冻结最终段基准落点。</summary>
        public bool HasFrozenFinalDestination { get; set; }

        /// <summary>冻结下来的最终段基准落点。</summary>
        public Vector3 FrozenFinalDestination { get; set; }

        /// <summary>清空当前投射物路径上下文。</summary>
        public void Reset()
        {
            Anchors.Clear();
            HasFinalTarget = false;
            FinalTarget = LocalTargetInfo.Invalid;
            FinalIsThing = false;
            PathSource = RoutePathSource.Direct;
            CurrentLegIndex = 0;
            AssignedEmitIndex = -1;
            ArrivalTolerance = 0.35f;
            IntermediateSpreadRadius = 0.625f;
            FinalSpreadRadius = 0.30f;
            HighAccuracySpreadScale = 0.25f;
            SpreadSafetyShrinkSteps = 4;
            HasFrozenFinalDestination = false;
            FrozenFinalDestination = Vector3.zero;
        }

        /// <summary>复制当前投射物路径上下文。</summary>
        public RoutePathContext CloneTyped()
        {
            RoutePathContext clone = new RoutePathContext
            {
                HasFinalTarget = HasFinalTarget,
                FinalTarget = FinalTarget,
                FinalIsThing = FinalIsThing,
                PathSource = PathSource,
                CurrentLegIndex = CurrentLegIndex,
                AssignedEmitIndex = AssignedEmitIndex,
                ArrivalTolerance = ArrivalTolerance,
                IntermediateSpreadRadius = IntermediateSpreadRadius,
                FinalSpreadRadius = FinalSpreadRadius,
                HighAccuracySpreadScale = HighAccuracySpreadScale,
                SpreadSafetyShrinkSteps = SpreadSafetyShrinkSteps,
                HasFrozenFinalDestination = HasFrozenFinalDestination,
                FrozenFinalDestination = FrozenFinalDestination
            };

            for (int i = 0; i < Anchors.Count; i++)
            {
                if (Anchors[i] != null) clone.Anchors.Add(Anchors[i].CloneTyped());
            }

            return clone;
        }

        public void ExposeData()
        {
            List<PathAnchor> anchors = Anchors;
            bool hasFinalTarget = HasFinalTarget;
            LocalTargetInfo finalTarget = FinalTarget;
            bool finalIsThing = FinalIsThing;
            RoutePathSource pathSource = PathSource;
            int currentLegIndex = CurrentLegIndex;
            int assignedEmitIndex = AssignedEmitIndex;
            float arrivalTolerance = ArrivalTolerance;
            float intermediateSpreadRadius = IntermediateSpreadRadius;
            float finalSpreadRadius = FinalSpreadRadius;
            float highAccuracySpreadScale = HighAccuracySpreadScale;
            int spreadSafetyShrinkSteps = SpreadSafetyShrinkSteps;
            bool hasFrozenFinalDestination = HasFrozenFinalDestination;
            Vector3 frozenFinalDestination = FrozenFinalDestination;

            Scribe_Collections.Look(ref anchors, "anchors", LookMode.Deep);
            Scribe_Values.Look(ref hasFinalTarget, "hasFinalTarget", false);
            Scribe_TargetInfo.Look(ref finalTarget, "finalTarget");
            Scribe_Values.Look(ref finalIsThing, "finalIsThing", false);
            Scribe_Values.Look(ref pathSource, "pathSource", RoutePathSource.Direct);
            Scribe_Values.Look(ref currentLegIndex, "currentLegIndex", 0);
            Scribe_Values.Look(ref assignedEmitIndex, "assignedEmitIndex", -1);
            Scribe_Values.Look(ref arrivalTolerance, "arrivalTolerance", 0.35f);
            Scribe_Values.Look(ref intermediateSpreadRadius, "intermediateSpreadRadius", 1.25f);
            Scribe_Values.Look(ref finalSpreadRadius, "finalSpreadRadius", 0.30f);
            Scribe_Values.Look(ref highAccuracySpreadScale, "highAccuracySpreadScale", 0.25f);
            Scribe_Values.Look(ref spreadSafetyShrinkSteps, "spreadSafetyShrinkSteps", 4);
            Scribe_Values.Look(ref hasFrozenFinalDestination, "hasFrozenFinalDestination", false);
            Scribe_Values.Look(ref frozenFinalDestination, "frozenFinalDestination");

            Anchors = anchors ?? new List<PathAnchor>();
            HasFinalTarget = hasFinalTarget;
            FinalTarget = finalTarget;
            FinalIsThing = finalIsThing;
            PathSource = pathSource;
            CurrentLegIndex = currentLegIndex < 0 ? 0 : currentLegIndex;
            AssignedEmitIndex = assignedEmitIndex;
            ArrivalTolerance = arrivalTolerance;
            IntermediateSpreadRadius = Mathf.Max(0f, intermediateSpreadRadius);
            FinalSpreadRadius = Mathf.Max(0f, finalSpreadRadius);
            HighAccuracySpreadScale = Mathf.Clamp01(highAccuracySpreadScale);
            SpreadSafetyShrinkSteps = Mathf.Clamp(spreadSafetyShrinkSteps, 0, 8);
            HasFrozenFinalDestination = hasFrozenFinalDestination;
            FrozenFinalDestination = frozenFinalDestination;
        }
    }
}
