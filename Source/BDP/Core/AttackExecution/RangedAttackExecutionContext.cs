using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;
using BDP.Core.VerbHosting;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一次远程正式执行在进入运行时前整理出的最小上下文。
    /// 它只回答当前 runtime step 该怎么交给远程宿主消费，不承担表达真值职责。
    /// </summary>
    internal sealed class RangedAttackExecutionContext
    {
        /// <summary>
        /// 当前会话命中的投影版本号。
        /// 它用于后续续接时确认运行中的远程会话仍落在同一轮已发布真值上。
        /// </summary>
        public int ProjectionVersion { get; private set; }

        /// <summary>
        /// 当前执行绑定的正式 plan。
        /// 它描述高层怎么编排，不直接等于宿主下一步怎么打。
        /// </summary>
        public AttackExecutionPlan Plan { get; private set; }

        /// <summary>
        /// 当前真正要交给远程宿主消费的运行时动作步。
        /// </summary>
        public AttackRuntimeStep Step { get; private set; }

        /// <summary>
        /// 当前执行的宿主 Pawn。
        /// </summary>
        public Pawn Pawn { get; private set; }

        /// <summary>
        /// 当前会话宿主对应的正式结果。
        /// 它回答“这次远程会话由哪条攻击入口承接”。
        /// </summary>
        public FormalExpressionResult SessionResult { get; private set; }

        /// <summary>
        /// 当前运行时动作步挂靠的宿主结果标识。
        /// </summary>
        public string HostResultId { get; private set; }

        /// <summary>
        /// 当前实际要执行的 cast 对应的正式结果。
        /// 它回答“当前这一次施放动作属于哪条结果”。
        /// </summary>
        public FormalExpressionResult Result { get; private set; }

        /// <summary>
        /// 当前实际要执行结果对应的正式运行时 Verb 规格。
        /// </summary>
        public ResolvedVerbSpec ResolvedVerbSpec { get; private set; }

        /// <summary>
        /// 当前动作步中首条 cast 对应的计划层施放动作。
        /// 它只保留给仍需读取首发施放事实的本地执行流程，真正的编排真值仍是 Step。
        /// </summary>
        public AttackExecutionCast Cast { get; private set; }

        /// <summary>
        /// 当前实际用于发射的目标。
        /// </summary>
        public LocalTargetInfo Target { get; private set; }

        /// <summary>
        /// 当前攻击会话确认的稳定目标。
        /// 它用于续射重建，不随单次发射的首段目标被覆盖。
        /// </summary>
        public LocalTargetInfo SessionTarget { get; private set; }

        /// <summary>
        /// 当前绑定的真实 Verb。
        /// </summary>
        public Verb Verb { get; private set; }

        /// <summary>
        /// 当前远程协议前半段已经整理出的正式结果。
        /// 它才是后续 Verb 发射桥真正应该消费的真值。
        /// </summary>
        public RangedAttackProtocolResult ProtocolResult { get; private set; }

        /// <summary>
        /// 当前整份请求映射后共有多少个远程运行时动作步。
        /// </summary>
        public int PlannedStepCount { get; private set; }

        /// <summary>
        /// 当前 cast 内会产生多少个效果实例。
        /// </summary>
        public int EmitCount { get; private set; }

        /// <summary>
        /// 当前运行时至少需要启动多少次真正开枪动作。
        /// </summary>
        public int RequiredStepCount { get; private set; }

        /// <summary>
        /// 当前这次执行是否来自正式强制攻击下单。
        /// </summary>
        public bool IsForceTargetOrder { get; private set; }

        /// <summary>
        /// 当前远程 cast 是否需要持续推进器承接。
        /// </summary>
        public bool RequiresContinuousDriver { get; private set; }

        /// <summary>
        /// 尝试为当前远程请求整理最小执行上下文。
        /// </summary>
        public static bool TryCreate(AttackExecutionPreparedContext request, out RangedAttackExecutionContext context)
        {
            return TryCreateForStep(request, request != null && request.RuntimeSteps != null && request.RuntimeSteps.Count > 0 ? request.RuntimeSteps[0] : null, out context);
        }

        /// <summary>
        /// 尝试为指定远程动作步整理最小执行上下文。
        /// </summary>
        public static bool TryCreateForStep(
            AttackExecutionPreparedContext request,
            AttackRuntimeStep step,
            out RangedAttackExecutionContext context)
        {
            context = null;
            if (request?.Request?.Pawn == null)
            {
                return false;
            }

            FormalExpressionResult sessionResult = request.Result;
            AttackExecutionCast primaryCast = step != null && step.Casts != null && step.Casts.Count > 0 ? step.Casts[0] : null;
            FormalExpressionResult result = primaryCast != null ? primaryCast.Result : sessionResult;
            string hostResultId = step != null && !string.IsNullOrWhiteSpace(step.HostResultId)
                ? step.HostResultId
                : sessionResult != null ? sessionResult.Id : null;
            LocalTargetInfo target = step != null && step.Target.IsValid ? step.Target : request.Request.Target;
            if (sessionResult == null || result == null || !target.IsValid || string.IsNullOrWhiteSpace(hostResultId))
            {
                return false;
            }

            if (!VerbHostSurfaceAccess.TryGetByResultId(request.Request.Pawn, hostResultId, out BdpFormalVerbBinding binding))
            {
                return false;
            }

            Verb formalVerb = binding.ResolveActiveVerb();
            if (formalVerb == null)
            {
                return false;
            }

            int plannedStepCount = ResolvePlannedCastCount(request);
            bool isForceTargetOrder = IsPersistentAttackOrder(request.DispatchIntent);
            context = new RangedAttackExecutionContext
            {
                ProjectionVersion = request.ProjectionVersion,
                Plan = request.Plan,
                Step = step,
                Pawn = request.Request.Pawn,
                SessionResult = sessionResult,
                HostResultId = hostResultId,
                Result = result,
                ResolvedVerbSpec = result.ResolvedVerbSpec,
                Cast = primaryCast,
                Target = target,
                SessionTarget = request.Request.Target,
                Verb = formalVerb,
                PlannedStepCount = plannedStepCount,
                EmitCount = step?.Emits != null ? step.Emits.Count : 1,
                RequiredStepCount = ResolveRequiredCastCount(plannedStepCount, isForceTargetOrder),
                IsForceTargetOrder = isForceTargetOrder,
                RequiresContinuousDriver = ResolveRequiresContinuousDriver(request, plannedStepCount, isForceTargetOrder)
            };
            return true;
        }

        /// <summary>
        /// 读取当前正式请求映射后还需推进多少个远程动作步。
        /// </summary>
        private static int ResolvePlannedCastCount(AttackExecutionPreparedContext request)
        {
            if (request?.RuntimeSteps == null)
            {
                return 1;
            }

            return request.RuntimeSteps.Count > 0
                ? request.RuntimeSteps.Count
                : 1;
        }

        /// <summary>
        /// 读取当前远程步骤至少需要启动多少次施放动作。
        /// </summary>
        private static int ResolveRequiredCastCount(int plannedCastCount, bool isForceTargetOrder)
        {
            if (isForceTargetOrder)
            {
                return int.MaxValue;
            }

            return 1;
        }

        /// <summary>
        /// 判断当前派单语义是否属于正式持续攻击命令。
        /// </summary>
        private static bool IsPersistentAttackOrder(AttackDispatchIntent dispatchIntent)
        {
            return dispatchIntent == AttackDispatchIntent.ForceTargetOrder
                || dispatchIntent == AttackDispatchIntent.AutoAttackOrder;
        }

        /// <summary>
        /// 判断当前远程 cast 是否需要交给持续推进器。
        /// </summary>
        private static bool ResolveRequiresContinuousDriver(
            AttackExecutionPreparedContext request,
            int plannedCastCount,
            bool isForceTargetOrder)
        {
            if (isForceTargetOrder)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 把当前远程协议前半段结果绑定到执行上下文。
        /// 从这一步起，宿主桥只应消费协议结果，不再回到旧的临时拼装。
        /// </summary>
        public void BindProtocolResult(RangedAttackProtocolResult protocolResult)
        {
            ProtocolResult = protocolResult;
            EmitCount = protocolResult?.VerbEmissionPlan != null && protocolResult.VerbEmissionPlan.ExpectedEmitCount > 0
                ? protocolResult.VerbEmissionPlan.ExpectedEmitCount
                : protocolResult?.ProjectilePlans != null && protocolResult.ProjectilePlans.Count > 0
                    ? protocolResult.ProjectilePlans.Count
                    : EmitCount;
        }
    }
}
