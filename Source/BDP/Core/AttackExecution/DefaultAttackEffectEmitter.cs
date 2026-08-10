using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.Expressions;
using BDP.Core.Verbs;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 默认攻击效果派发器。
    /// 它负责把一个执行组中的 cast 真正派发到远程或近战武器层。
    /// </summary>
    internal sealed class AttackEffectEmitter
    {
        /// <summary>
        /// 尝试派发一个正式执行组。
        /// </summary>
        public bool TryEmitGroup(AttackExecutionPreparedContext request, AttackExecutionGroup group)
        {
            if (request?.Pawn == null || group == null || request.RuntimeSteps == null || request.RuntimeSteps.Count == 0)
            {
                return false;
            }

            bool emittedAny = false;
            for (int i = 0; i < request.RuntimeSteps.Count; i++)
            {
                AttackRuntimeStep step = request.RuntimeSteps[i];
                if (step == null || step.GroupIndex != group.GroupIndex)
                {
                    continue;
                }

                if (!TryEmitStep(request, step))
                {
                    return emittedAny;
                }

                emittedAny = true;
            }

            return emittedAny;
        }

        /// <summary>
        /// 尝试派发一个最小运行时动作步。
        /// </summary>
        private static bool TryEmitStep(AttackExecutionPreparedContext request, AttackRuntimeStep step)
        {
            return step.WeaponMode == WeaponExpressionMode.Ranged
                ? TryEmitRangedStep(request, step)
                : TryEmitMeleeStep(request, step);
        }

        /// <summary>
        /// 派发一个远程 runtime step。
        /// </summary>
        private static bool TryEmitRangedStep(
            AttackExecutionPreparedContext request,
            AttackRuntimeStep step)
        {
            if (!RangedAttackExecutionContext.TryCreateForStep(request, step, out RangedAttackExecutionContext context))
            {
                return false;
            }

            if (!(context.Verb is BdpVerb_Shoot shootVerb))
            {
                return false;
            }

            RangedAttackProtocolService protocolService = RangedAttackProtocolSurfaceAccess.Resolve(request.Pawn);
            if (protocolService == null)
            {
                return false;
            }
            if (!protocolService.TryBuild(request, context.Step, context.Result, out var protocolResult))
            {
                return false;
            }

            context.BindProtocolResult(protocolResult);
            shootVerb.ApplyExecutionContext(context);
            shootVerb.BindVerbEmissionPlan(protocolResult.VerbEmissionPlan);
            if (protocolResult.ProjectilePlans == null || protocolResult.ProjectilePlans.Count == 0)
            {
                return false;
            }

            bool emittedAny = false;
            for (int i = 0; i < protocolResult.ProjectilePlans.Count; i++)
            {
                if (!shootVerb.TryEmitPlan(protocolResult.ProjectilePlans[i]))
                {
                    return emittedAny;
                }

                emittedAny = true;
            }

            return emittedAny;
        }

        /// <summary>
        /// 派发一个近战 runtime step。
        /// </summary>
        private static bool TryEmitMeleeStep(AttackExecutionPreparedContext request, AttackRuntimeStep step)
        {
            if (!MeleeAttackExecutionContext.TryCreateForStep(request, step, out MeleeAttackExecutionContext context))
            {
                return false;
            }

            if (!(context.Verb is BdpVerb_MeleeAttackDamage meleeVerb))
            {
                return false;
            }

            meleeVerb.ApplyExecutionContext(context);
            return context.Target.IsValid && meleeVerb.TryStartCastOn(context.Target);
        }
    }
}
