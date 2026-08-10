using BDP.Core.Expressions;
using BDP.Core.Verbs;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 第一版默认近战攻击执行器。
    /// 当前阶段负责把正式近战结果解析成真实 Verb，并分流到直接施放或 BDP 持续推进链。
    /// </summary>
    internal sealed class MeleeAttackExecutor
    {
        /// <summary>
        /// 尝试把当前近战请求送入正式近战执行链。
        /// </summary>
        public bool TryExecute(AttackExecutionPreparedContext request)
        {
            if (!CanExecute(request))
            {
                AttackExecutionDiagnostics.LogRejected(request != null ? request.Request : null, "melee_executor_reject");
                return false;
            }

            if (!MeleeAttackExecutionContext.TryCreate(request, out MeleeAttackExecutionContext context))
            {
                AttackExecutionDiagnostics.LogRejected(request.Request, "melee_context_create_failed");
                return false;
            }

            BindVerbContext(context);
            AttackExecutionDiagnostics.LogMeleeExecutionStart(context, request.DispatchIntent);
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
        /// 判断当前请求是否满足近战执行器最小接单条件。
        /// </summary>
        private static bool CanExecute(AttackExecutionPreparedContext request)
        {
            return request != null
                && request.Request != null
                && request.Request.Pawn != null
                && request.Request.Target.IsValid
                && request.Result != null
                && request.Result.ResultKind == ExpressionResultKind.Verb
                && request.Result.WeaponMode == WeaponExpressionMode.Melee;
        }

        /// <summary>
        /// 把正式结果上下文绑定到本次真实近战 Verb 宿主上。
        /// </summary>
        internal static void BindVerbContext(MeleeAttackExecutionContext context)
        {
            if (context?.Verb is BdpVerb_MeleeAttackDamage meleeVerb)
            {
                meleeVerb.ApplyExecutionContext(context);
            }

            if (context?.Verb is BdpVerb_FormalHostMelee formalHost)
            {
                formalHost.ApplyStepToolSurface(stepIndex: 0);
            }
        }

        /// <summary>
        /// 把当前近战步骤正式交给 JobDriver 持续推进。
        /// </summary>
        private static bool TryStartContinuousJob(MeleeAttackExecutionContext context)
        {
            if (context?.Pawn?.jobs == null || !context.Target.HasThing)
            {
                return false;
            }

            if (context.NextRuntimeStepIndex >= 0 && context.PlanSessionToken == null)
            {
                return false;
            }

            JobDef jobDef = AttackExecutionJobDefs.MeleeAttackExecution;
            if (jobDef == null)
            {
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, context.Target);
            job.verbToUse = context.Verb;
            job.maxNumMeleeAttacks = context.RequiredStepCount;
            return context.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
