using BDP.Core.Expressions;
using BDP.Core.Combos;
using BDP.Core.Requirements;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// AttackExecution 正式服务。
    /// 当前阶段它负责把外部请求收口、校验、解析、编排，并分流到近战或远程执行器。
    /// </summary>
    internal sealed partial class AttackExecutionService
    {
        /// <summary>
        /// 当前服务绑定的效果派发器。
        /// </summary>
        private readonly AttackEffectEmitter effectEmitter;

        /// <summary>
        /// 当前服务绑定的远程执行器。
        /// </summary>
        private readonly RangedAttackExecutor rangedAttackExecutor;

        /// <summary>
        /// 当前服务绑定的近战执行器。
        /// </summary>
        private readonly MeleeAttackExecutor meleeAttackExecutor;

        /// <summary>
        /// 用指定最终执行分支构造正式执行服务。
        /// </summary>
        internal AttackExecutionService(
            AttackEffectEmitter effectEmitter = null,
            RangedAttackExecutor rangedAttackExecutor = null,
            MeleeAttackExecutor meleeAttackExecutor = null)
        {
            this.effectEmitter = effectEmitter ?? new AttackEffectEmitter();
            this.rangedAttackExecutor = rangedAttackExecutor ?? new RangedAttackExecutor();
            this.meleeAttackExecutor = meleeAttackExecutor ?? new MeleeAttackExecutor();
        }

        /// <summary>
        /// 尝试接收、校验并分流一条正式攻击请求。
        /// 当前阶段只支持 Verb 类正式结果进入近战或远程正式执行链。
        /// ImmediateCast 走直接施放，ForceTargetOrder 走 BDP 自己的持续推进 job 链。
        /// </summary>
        public bool TryExecute(AttackExecutionRequest request)
        {
            if (!CanAccept(request))
            {
                AttackExecutionDiagnostics.LogRejected(request, "request_not_acceptable");
                return false;
            }

            NormalizeDispatchIntent(request);
            EnsureAttackInstanceId(request);

            if (!TryPrepareContext(request, out AttackExecutionPreparedContext preparedContext))
            {
                AttackExecutionDiagnostics.LogRejected(request, "resolve_failed");
                return false;
            }

            if (!TryBuildPlan(preparedContext, out AttackExecutionPlan plan))
            {
                AttackExecutionDiagnostics.LogRejected(request, "plan_build_failed");
                return false;
            }

            preparedContext.Plan = plan;
            if (!TryBuildSteps(preparedContext, out var runtimeSteps)
                || runtimeSteps == null
                || runtimeSteps.Count == 0)
            {
                AttackExecutionDiagnostics.LogRejected(request, "runtime_step_build_failed");
                return false;
            }

            preparedContext.RuntimeSteps = runtimeSteps;
            return TryExecutePrepared(preparedContext);
        }

        /// <summary>
        /// 为既有攻击会话准备一整轮正式 plan。
        /// 这条口只负责解析与编排，不创建新的攻击推进会话。
        /// </summary>
        public bool TryPreparePlan(
            AttackExecutionRequest request,
            out AttackExecutionPreparedContext preparedContext)
        {
            preparedContext = null;
            if (!CanAccept(request))
            {
                return false;
            }

            NormalizeDispatchIntent(request);
            EnsureAttackInstanceId(request);
            if (!TryPrepareContext(request, out preparedContext))
            {
                return false;
            }

            if (!TryBuildPlan(preparedContext, out AttackExecutionPlan plan))
            {
                return false;
            }

            preparedContext.Plan = plan;
            if (!TryBuildSteps(preparedContext, out var runtimeSteps)
                || runtimeSteps == null
                || runtimeSteps.Count == 0)
            {
                return false;
            }

            preparedContext.RuntimeSteps = runtimeSteps;
            return true;
        }

        /// <summary>
        /// 为既有攻击会话内的一次真实施放准备首条计划层 cast。
        /// 当前兼容口仍返回 cast，但它已经来自 runtime step 的首条映射结果。
        /// </summary>
        public bool TryPrepareCast(
            AttackExecutionRequest request,
            out AttackExecutionPreparedContext preparedContext,
            out AttackExecutionCast cast)
        {
            preparedContext = null;
            cast = null;
            if (!TryPreparePlan(request, out preparedContext))
            {
                return false;
            }

            cast = preparedContext.RuntimeSteps != null
                && preparedContext.RuntimeSteps.Count > 0
                && preparedContext.RuntimeSteps[0] != null
                && preparedContext.RuntimeSteps[0].Casts != null
                && preparedContext.RuntimeSteps[0].Casts.Count > 0
                ? preparedContext.RuntimeSteps[0].Casts[0]
                : null;
            return cast != null;
        }

        /// <summary>
        /// 判断当前请求是否满足入口最小接单条件。
        /// </summary>
        private static bool CanAccept(AttackExecutionRequest request)
        {
            AttackSessionToken sessionToken = request != null ? request.SessionToken : null;
            return request != null
                && request.Pawn != null
                && !request.Pawn.WorkTagIsDisabled(WorkTags.Violent)
                && sessionToken != null
                && sessionToken.IsValid
                && sessionToken.BelongsTo(request.Pawn);
        }

        /// <summary>
        /// 把入口未显式填写的派单意图归一化为当前默认语义。
        /// 当前只有显式声明 ForceTargetOrder 或 AutoAttackOrder 的入口会走正式下单链，其余旧入口继续按 ImmediateCast 处理。
        /// </summary>
        private static void NormalizeDispatchIntent(AttackExecutionRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (request.DispatchIntent != AttackDispatchIntent.ForceTargetOrder
                && request.DispatchIntent != AttackDispatchIntent.AutoAttackOrder)
            {
                request.DispatchIntent = AttackDispatchIntent.ImmediateCast;
            }
        }

        /// <summary>
        /// 为当前请求补齐攻击实例标识。
        /// </summary>
        private static void EnsureAttackInstanceId(AttackExecutionRequest request)
        {
            if (request?.SessionToken == null || !string.IsNullOrWhiteSpace(request.AttackInstanceId))
            {
                return;
            }

            request.SessionToken = request.SessionToken.WithAttackInstanceId(AttackInstanceIdFactory.Create());
        }
        /// <summary>
        /// 尝试把正式请求解析成绑定已发布投影的准备上下文。
        /// 这条路径只认当前 Trigger 已发布的战斗真值，不再回头重算表达快照。
        /// </summary>
        private static bool TryPrepareContext(
            AttackExecutionRequest request,
            out AttackExecutionPreparedContext preparedContext)
        {
            preparedContext = null;
            if (request?.Pawn == null)
            {
                return false;
            }

            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(request.Pawn);
            TriggerCombatProjectionState projection = triggerBody != null ? triggerBody.PublishedCombatProjection : null;
            AttackSessionToken sessionToken = request != null ? request.SessionToken : null;
            if (projection == null
                || projection.IsEmpty
                || projection.ProjectionVersion <= 0
                || sessionToken == null
                || sessionToken.ProjectionVersion != projection.ProjectionVersion
                || !sessionToken.BelongsTo(request.Pawn)
                || projection.ResultIndex == null
                || !projection.ResultIndex.TryGetValue(sessionToken.ResultId, out FormalExpressionResult result)
                || result == null
                || !result.IsAvailable)
            {
                return false;
            }

            // 在任何计划、扣费或动作开始前实时复查 Combo 自己的角色使用条件。
            if (!string.IsNullOrWhiteSpace(result.ComboDefName))
            {
                PawnRequirementCheckResult requirementCheck =
                    ComboUseRequirementService.Instance.Evaluate(request.Pawn, result.ComboDefName);
                if (!requirementCheck.Satisfied)
                {
                    return false;
                }
            }

            preparedContext = new AttackExecutionPreparedContext
            {
                Request = request,
                Projection = projection,
                Result = result
            };
            return true;
        }
    }
}
