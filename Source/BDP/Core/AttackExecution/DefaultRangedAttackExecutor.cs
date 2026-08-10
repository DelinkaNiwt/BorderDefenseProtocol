using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;
using BDP.Core.Verbs;
using Verse.AI;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 第一版默认远程攻击执行器。
    /// 当前阶段负责把正式远程结果解析成真实 Verb，并分流到直接施放或 BDP 持续推进链。
    /// </summary>
    internal sealed class RangedAttackExecutor
    {
        /// <summary>
        /// 尝试把当前远程请求送入正式远程执行链。
        /// </summary>
        public bool TryExecute(AttackExecutionPreparedContext request)
        {
            if (!CanExecute(request))
            {
                AttackExecutionDiagnostics.LogRejected(request != null ? request.Request : null, "ranged_executor_reject");
                return false;
            }

            if (!RangedAttackExecutionContext.TryCreate(request, out RangedAttackExecutionContext context))
            {
                AttackExecutionDiagnostics.LogRejected(request.Request, "ranged_context_create_failed");
                return false;
            }

            RangedAttackProtocolService rangedAttackProtocolService =
                RangedAttackProtocolSurfaceAccess.Resolve(request.Pawn);
            if (rangedAttackProtocolService == null)
            {
                AttackExecutionDiagnostics.LogRejected(request.Request, "ranged_protocol_service_missing");
                return false;
            }

            if (!rangedAttackProtocolService.TryBuild(request, context.Step, context.Result, out var protocolResult))
            {
                RangedAttackProtocolDiagnostics.LogFailure("protocol_build_failed", request);
                return false;
            }

            context.BindProtocolResult(protocolResult);
            RangedBurstEmissionAssembler.TryBuild(
                request,
                context,
                protocolResult,
                rangedAttackProtocolService,
                out RangedVerbEmissionPlan immediateEmissionPlan);
            BindVerbContext(context, immediateEmissionPlan);
            AttackExecutionDiagnostics.LogRangedExecutionStart(context, request.DispatchIntent);
            if (request.DispatchIntent == AttackDispatchIntent.ForceTargetOrder)
            {
                return TryStartContinuousJob(context);
            }

            if (context.RequiresContinuousDriver)
            {
                return TryStartContinuousJob(context);
            }

            return context.Target.IsValid
                && context.Verb.TryStartCastOn(context.Target);
        }

        /// <summary>
        /// 判断当前请求是否满足远程执行器最小接单条件。
        /// </summary>
        private static bool CanExecute(AttackExecutionPreparedContext request)
        {
            return request != null
                && request.Request != null
                && request.Request.Pawn != null
                && request.Request.Target.IsValid
                && request.Result != null
                && request.Result.ResultKind == ExpressionResultKind.Verb
                && request.Result.WeaponMode == WeaponExpressionMode.Ranged;
        }

        /// <summary>
        /// 把正式结果上下文绑定到本次真实远程 Verb 宿主上。
        /// </summary>
        private static void BindVerbContext(
            RangedAttackExecutionContext context,
            RangedVerbEmissionPlan immediateEmissionPlan)
        {
            if (context?.Verb is BdpVerb_Shoot shootVerb)
            {
                shootVerb.ApplyExecutionContext(context);
                shootVerb.BindVerbEmissionPlan(immediateEmissionPlan);
            }
        }

        /// <summary>
        /// 把当前远程步骤正式交给 JobDriver 持续推进。
        /// </summary>
        private static bool TryStartContinuousJob(RangedAttackExecutionContext context)
        {
            if (context?.Pawn?.jobs == null)
            {
                return false;
            }

            JobDef jobDef = AttackExecutionJobDefs.RangedAttackExecution;
            if (jobDef == null)
            {
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, context.Target);
            job.verbToUse = context.Verb;
            job.maxNumStaticAttacks = context.RequiredStepCount;
            // 对齐原版持续远程攻击：目标暂时超出射程或失去视线时保留 job，
            // 直到目标重新满足当前射击条件，而不是把攻击订单直接判为不可完成。
            job.endIfCantShootTargetFromCurPos = false;
            job.preventFriendlyFire = false;
            return context.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
