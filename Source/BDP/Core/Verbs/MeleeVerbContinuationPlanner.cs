using BDP.Core.AttackExecution;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// 近战宿主续段规划器。
    /// 它负责在当前 run（连续段）打完后，按原始正式计划重建下一段近战上下文。
    /// </summary>
    internal sealed class MeleeVerbContinuationPlanner
    {
        /// <summary>
        /// 为指定近战宿主准备下一段待消费的近战 run。
        /// </summary>
        internal bool TryPrepareNextRun(
            BdpVerb_MeleeAttackDamage verb,
            LocalTargetInfo target,
            out MeleeAttackExecutionContext context)
        {
            context = null;
            if (verb == null
                || !(verb.Caster is Pawn pawn)
                || verb.PlanSessionToken == null
                || !verb.PlanSessionToken.IsValid
                || verb.NextRuntimeStepIndex < 0)
            {
                return false;
            }

            if (!AttackExecutionPostLoadRecovery.IsCurrentAttackSessionValid(verb))
            {
                return false;
            }

            AttackExecutionService entry = AttackExecutionSurfaceAccess.ResolveEntry(pawn);
            if (entry == null)
            {
                return false;
            }

            if (!entry.TryPreparePlan(
                    new AttackExecutionRequest
                    {
                        Pawn = pawn,
                        SessionToken = verb.PlanSessionToken,
                        AttackContextSnapshot = verb.PlanAttackContextSnapshot,
                        Target = target,
                        Reason = verb.PlanReason,
                        DispatchIntent = verb.PlanDispatchIntent
                    },
                    out AttackExecutionPreparedContext preparedContext))
            {
                return false;
            }

            if (!MeleeAttackExecutionContext.TryCreateForStepIndex(
                    preparedContext,
                    verb.NextRuntimeStepIndex,
                    out context))
            {
                return false;
            }

            MeleeAttackExecutor.BindVerbContext(context);
            return true;
        }
    }
}
