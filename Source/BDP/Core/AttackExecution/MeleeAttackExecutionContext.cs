using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.VerbHosting;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一次近战正式执行在进入运行时前整理出的最小上下文。
    /// 它只回答当前 runtime step 该砍谁、要不要追，不承担表达真值职责。
    /// </summary>
    internal sealed class MeleeAttackExecutionContext
    {
        /// <summary>
        /// 当前会话命中的投影版本号。
        /// 它用于把近战执行上下文绑定回同一轮已发布战斗真值。
        /// </summary>
        public int ProjectionVersion { get; private set; }

        public Pawn Pawn { get; private set; }

        /// <summary>
        /// 当前整次近战计划对应的正式会话令牌。
        /// 它服务 run（连续段）之间的正式续接，不等于当前宿主壳身份。
        /// </summary>
        public AttackSessionToken PlanSessionToken { get; private set; }

        /// <summary>
        /// 当前整次近战计划冻结下来的攻击上下文快照。
        /// 它用于后续 run 续接时重建正式请求，不回表达层重算业务。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot { get; private set; }

        /// <summary>
        /// 当前 run 打完后应从哪一个 runtime step 继续。
        /// 没有后续时为 -1；持续攻击整轮收口后可回到 0。
        /// </summary>
        public int NextRuntimeStepIndex { get; private set; }

        /// <summary>
        /// 当前整次计划的派单意图。
        /// 它用于 run 续接时保持与首段一致的正式语义。
        /// </summary>
        public AttackDispatchIntent PlanDispatchIntent { get; private set; }

        /// <summary>
        /// 当前整次计划最初来自哪条正式入口。
        /// 它只服务续接重建请求与诊断，不承担业务判断。
        /// </summary>
        public AttackExecutionReason PlanReason { get; private set; }

        public FormalExpressionResult Result { get; private set; }

        /// <summary>
        /// 当前近战结果对应的正式运行时 Verb 规格。
        /// </summary>
        public ResolvedVerbSpec ResolvedVerbSpec { get; private set; }

        public AttackRuntimeStep Step { get; private set; }

        public AttackExecutionCast Cast { get; private set; }

        public LocalTargetInfo Target { get; private set; }

        public Verb Verb { get; private set; }

        public int PlannedStepCount { get; private set; }

        public int RequiredStepCount { get; private set; }

        public IReadOnlyList<int> StepIntervalTicks { get; private set; }

        /// <summary>
        /// 当前轮已经预排好的 step-tool 索引序列。
        /// 它只描述这一轮每刀该绑定哪把 Tool，不承担表达真值职责。
        /// </summary>
        public IReadOnlyList<int> PreparedStepToolIndices { get; private set; }

        public bool IsForceTargetOrder { get; private set; }

        public bool RequiresChase { get; private set; }

        public bool RequiresContinuousDriver { get; private set; }

        /// <summary>
        /// 尝试为当前近战请求整理最小执行上下文。
        /// </summary>
        public static bool TryCreate(AttackExecutionPreparedContext request, out MeleeAttackExecutionContext context)
        {
            return TryCreateForStepIndex(request, 0, out context);
        }

        /// <summary>
        /// 尝试为指定近战动作步整理最小执行上下文。
        /// </summary>
        public static bool TryCreateForStep(
            AttackExecutionPreparedContext request,
            AttackRuntimeStep step,
            out MeleeAttackExecutionContext context)
        {
            return TryCreateForStepIndex(request, FindStepIndex(request, step), out context);
        }

        /// <summary>
        /// 尝试从指定 step 索引开始，为当前连续近战 run 整理最小执行上下文。
        /// </summary>
        public static bool TryCreateForStepIndex(
            AttackExecutionPreparedContext request,
            int startStepIndex,
            out MeleeAttackExecutionContext context)
        {
            context = null;
            if (request?.Request?.Pawn == null)
            {
                return false;
            }

            List<AttackRuntimeStep> runSteps = CollectRunSteps(request, startStepIndex);
            if (runSteps == null || runSteps.Count == 0)
            {
                return false;
            }

            AttackRuntimeStep step = runSteps[0];
            AttackExecutionCast cast = ResolvePrimaryCast(step);
            FormalExpressionResult result = cast != null ? cast.Result : request.Result;
            LocalTargetInfo target = step != null && step.Target.IsValid ? step.Target : request.Request.Target;
            if (result == null || !target.IsValid)
            {
                return false;
            }

            if (!VerbHostSurfaceAccess.TryGetByResultId(request.Request.Pawn, result.Id, out BdpFormalVerbBinding binding))
            {
                return false;
            }

            Verb formalVerb = binding.ResolveActiveVerb();
            if (formalVerb == null)
            {
                return false;
            }

            int plannedCastCount = ResolvePlannedCastCount(runSteps);
            IReadOnlyList<int> stepIntervals = ResolveStepIntervalTicks(runSteps);
            IReadOnlyList<int> preparedStepToolIndices = VanillaCompatibleMeleeToolSelector.PrepareStepToolSequence(
                request.Request.Pawn,
                target,
                result,
                ResolveCandidateToolSurfaces(binding, result),
                plannedCastCount,
                request.AttackInstanceId,
                roundOrdinal: 0);
            bool requiresChase = ResolveRequiresChase(request.Request.Pawn, target);
            bool isForceTargetOrder = IsPersistentAttackOrder(request.DispatchIntent);
            int nextRuntimeStepIndex = ResolveNextRuntimeStepIndex(request, startStepIndex + plannedCastCount, isForceTargetOrder);
            context = new MeleeAttackExecutionContext
            {
                ProjectionVersion = request.ProjectionVersion,
                Pawn = request.Request.Pawn,
                PlanSessionToken = request.SessionToken != null ? request.SessionToken.Clone() : null,
                AttackContextSnapshot = request.AttackContextSnapshot,
                NextRuntimeStepIndex = nextRuntimeStepIndex,
                PlanDispatchIntent = request.DispatchIntent,
                PlanReason = request.Request.Reason,
                Result = result,
                ResolvedVerbSpec = result.ResolvedVerbSpec,
                Step = step,
                Cast = cast,
                Target = target,
                Verb = formalVerb,
                PlannedStepCount = plannedCastCount,
                RequiredStepCount = ResolveRequiredCastCount(plannedCastCount, isForceTargetOrder),
                StepIntervalTicks = stepIntervals,
                PreparedStepToolIndices = preparedStepToolIndices,
                IsForceTargetOrder = isForceTargetOrder,
                RequiresChase = requiresChase,
                RequiresContinuousDriver = ResolveRequiresContinuousDriver(
                    request,
                    plannedCastCount,
                    requiresChase,
                    isForceTargetOrder,
                    nextRuntimeStepIndex)
            };
            return true;
        }

        private static int ResolveRequiredCastCount(int plannedCastCount, bool isForceTargetOrder)
        {
            if (isForceTargetOrder)
            {
                return int.MaxValue;
            }

            return plannedCastCount > 0 ? plannedCastCount : 1;
        }

        private static int ResolvePlannedCastCount(IReadOnlyList<AttackRuntimeStep> runSteps)
        {
            return runSteps != null && runSteps.Count > 0
                ? runSteps.Count
                : 1;
        }

        private static IReadOnlyList<int> ResolveStepIntervalTicks(IReadOnlyList<AttackRuntimeStep> runSteps)
        {
            List<int> intervals = new List<int>();
            if (runSteps == null || runSteps.Count == 0)
            {
                intervals.Add(0);
                return intervals;
            }

            for (int i = 0; i < runSteps.Count; i++)
            {
                AttackRuntimeStep step = runSteps[i];
                intervals.Add(step != null ? step.IntervalTicksAfter : 0);
            }

            if (intervals.Count == 0)
            {
                intervals.Add(0);
            }

            return intervals;
        }

        private static int FindStepIndex(
            AttackExecutionPreparedContext request,
            AttackRuntimeStep step)
        {
            if (request?.RuntimeSteps == null || step == null)
            {
                return -1;
            }

            for (int i = 0; i < request.RuntimeSteps.Count; i++)
            {
                if (ReferenceEquals(request.RuntimeSteps[i], step))
                {
                    return i;
                }
            }

            if (step.StepIndex >= 0
                && step.StepIndex < request.RuntimeSteps.Count)
            {
                AttackRuntimeStep candidate = request.RuntimeSteps[step.StepIndex];
                if (candidate != null && candidate.StepIndex == step.StepIndex)
                {
                    return step.StepIndex;
                }
            }

            return -1;
        }

        private static List<AttackRuntimeStep> CollectRunSteps(
            AttackExecutionPreparedContext request,
            int startStepIndex)
        {
            if (request?.RuntimeSteps == null
                || startStepIndex < 0
                || startStepIndex >= request.RuntimeSteps.Count)
            {
                return null;
            }

            AttackRuntimeStep startStep = request.RuntimeSteps[startStepIndex];
            AttackExecutionCast startCast = ResolvePrimaryCast(startStep);
            string runResultId = startCast != null ? startCast.ResultId : null;
            if (startStep == null
                || startStep.WeaponMode != WeaponExpressionMode.Melee
                || string.IsNullOrWhiteSpace(runResultId))
            {
                return null;
            }

            List<AttackRuntimeStep> runSteps = new List<AttackRuntimeStep>();
            for (int i = startStepIndex; i < request.RuntimeSteps.Count; i++)
            {
                AttackRuntimeStep currentStep = request.RuntimeSteps[i];
                AttackExecutionCast currentCast = ResolvePrimaryCast(currentStep);
                if (currentStep == null
                    || currentStep.WeaponMode != WeaponExpressionMode.Melee
                    || currentCast == null
                    || currentCast.ResultId != runResultId)
                {
                    break;
                }

                runSteps.Add(currentStep);
            }

            return runSteps.Count > 0 ? runSteps : null;
        }

        private static AttackExecutionCast ResolvePrimaryCast(AttackRuntimeStep step)
        {
            return step != null && step.Casts != null && step.Casts.Count > 0
                ? step.Casts[0]
                : null;
        }

        private static int ResolveNextRuntimeStepIndex(
            AttackExecutionPreparedContext request,
            int nextDirectStepIndex,
            bool isForceTargetOrder)
        {
            if (request?.RuntimeSteps == null || request.RuntimeSteps.Count == 0)
            {
                return -1;
            }

            if (nextDirectStepIndex >= 0 && nextDirectStepIndex < request.RuntimeSteps.Count)
            {
                return nextDirectStepIndex;
            }

            return isForceTargetOrder
                ? 0
                : -1;
        }

        /// <summary>
        /// 解析当前结果可供 step 选择的 Tool 表面集合。
        /// 优先读取 formal binding 当前持有的候选表面，缺失时回退到结果快照自身。
        /// </summary>
        private static IReadOnlyList<MeleeToolSurface> ResolveCandidateToolSurfaces(
            BdpFormalVerbBinding binding,
            FormalExpressionResult result)
        {
            if (binding?.State?.DeclaredMeleeToolSurfaces != null
                && binding.State.DeclaredMeleeToolSurfaces.Count > 0)
            {
                return binding.State.DeclaredMeleeToolSurfaces;
            }

            return result != null
                ? result.DeclaredMeleeToolSurfaces
                : null;
        }

        private static bool IsPersistentAttackOrder(AttackDispatchIntent dispatchIntent)
        {
            return dispatchIntent == AttackDispatchIntent.ForceTargetOrder
                || dispatchIntent == AttackDispatchIntent.AutoAttackOrder;
        }

        private static bool ResolveRequiresChase(Pawn pawn, LocalTargetInfo target)
        {
            if (pawn == null || !target.IsValid)
            {
                return false;
            }

            if (target.HasThing)
            {
                return !pawn.CanReachImmediate(target.Thing, PathEndMode.Touch);
            }

            return !pawn.Position.AdjacentTo8WayOrInside(target.Cell);
        }

        private static bool ResolveRequiresContinuousDriver(
            AttackExecutionPreparedContext request,
            int plannedCastCount,
            bool requiresChase,
            bool isForceTargetOrder,
            int nextRuntimeStepIndex)
        {
            if (isForceTargetOrder)
            {
                return true;
            }

            if (request?.Plan != null && request.Plan.DriveMode == AttackDriveMode.Continuous)
            {
                return true;
            }

            if (requiresChase)
            {
                return true;
            }

            if (nextRuntimeStepIndex >= 0)
            {
                return true;
            }

            return plannedCastCount > 1;
        }
    }
}
