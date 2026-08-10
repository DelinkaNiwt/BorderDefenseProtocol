using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程默认 burst 发射计划装配器。
    /// 它只负责把同一轮顺序远程步骤回收成一份宿主可直接消费的 burst 计划。
    /// </summary>
    internal static class RangedBurstEmissionAssembler
    {
        /// <summary>
        /// 尝试基于首个协议结果装配一轮默认 burst 发射计划。
        /// 若当前请求不满足“回归原版 burst”条件，则直接回落为首步计划。
        /// </summary>
        public static bool TryBuild(
            AttackExecutionPreparedContext request,
            RangedAttackExecutionContext context,
            RangedAttackProtocolResult firstProtocolResult,
            RangedAttackProtocolService protocolService,
            out RangedVerbEmissionPlan emissionPlan)
        {
            emissionPlan = firstProtocolResult != null ? firstProtocolResult.VerbEmissionPlan : null;
            RangedVerbEmissionPlan firstStepPlan = emissionPlan;
            if (!ShouldAggregate(request, context, firstStepPlan) || protocolService == null)
            {
                return emissionPlan != null;
            }

            List<RangedVerbEmissionWindowPlan> windows = new List<RangedVerbEmissionWindowPlan>();
            List<string> sourceResultIds = new List<string>();
            int expectedEmitCount = 0;
            for (int i = 0; i < request.RuntimeSteps.Count; i++)
            {
                AttackRuntimeStep step = request.RuntimeSteps[i];
                if (!CanAppendStep(step, context))
                {
                    break;
                }

                RangedVerbEmissionPlan stepPlan;
                if (i == 0)
                {
                    stepPlan = firstStepPlan;
                }
                else
                {
                    if (!RangedAttackExecutionContext.TryCreateForStep(request, step, out RangedAttackExecutionContext stepContext))
                    {
                        emissionPlan = firstStepPlan;
                        return emissionPlan != null;
                    }

                    if (!protocolService.TryBuild(request, stepContext.Step, stepContext.Result, out RangedAttackProtocolResult stepProtocolResult))
                    {
                        emissionPlan = firstStepPlan;
                        return emissionPlan != null;
                    }

                    stepPlan = stepProtocolResult != null ? stepProtocolResult.VerbEmissionPlan : null;
                }

                AppendWindows(windows, stepPlan != null ? stepPlan.Windows : null);
                AppendSourceResultIds(sourceResultIds, stepPlan != null ? stepPlan.StepSourceResultIds : null);
                expectedEmitCount += stepPlan != null ? stepPlan.ExpectedEmitCount : 0;
            }

            if (windows.Count <= 1)
            {
                emissionPlan = firstStepPlan;
                return emissionPlan != null;
            }

            emissionPlan = new RangedVerbEmissionPlan
            {
                Windows = windows,
                StepAttackInstanceId = firstStepPlan != null ? firstStepPlan.StepAttackInstanceId : null,
                StepHostResultId = firstStepPlan != null ? firstStepPlan.StepHostResultId : null,
                StepSourceResultIds = sourceResultIds,
                ExpectedEmitCount = expectedEmitCount > 0 ? expectedEmitCount : CountExpectedEmits(windows)
            };
            return true;
        }

        /// <summary>
        /// 判断当前请求是否应把顺序远程步骤回收成一轮原版 burst 会话。
        /// </summary>
        private static bool ShouldAggregate(
            AttackExecutionPreparedContext request,
            RangedAttackExecutionContext context,
            RangedVerbEmissionPlan firstStepPlan)
        {
            return request != null
                && request.RuntimeSteps != null
                && request.RuntimeSteps.Count > 1
                && context != null
                && context.Verb is Verbs.BdpVerb_Shoot
                && context.Step != null
                && context.Result != null
                && firstStepPlan != null
                && firstStepPlan.Windows != null
                && firstStepPlan.Windows.Count > 0;
        }

        /// <summary>
        /// 判断后续 runtime step 是否仍属于同一轮默认顺序 burst。
        /// </summary>
        private static bool CanAppendStep(AttackRuntimeStep step, RangedAttackExecutionContext context)
        {
            if (step == null || context?.Step == null || context.Result == null)
            {
                return false;
            }

            AttackExecutionCast cast = step.Casts != null && step.Casts.Count > 0 ? step.Casts[0] : null;
            return step.WeaponMode == WeaponExpressionMode.Ranged
                && step.ExecutionKind == AttackGroupExecutionKind.VerbSession
                && step.HostResultId == context.Step.HostResultId
                && step.Target == context.Step.Target
                && cast != null;
        }

        /// <summary>
        /// 追加发射窗口，保持顺序。
        /// </summary>
        private static void AppendWindows(
            List<RangedVerbEmissionWindowPlan> target,
            IReadOnlyList<RangedVerbEmissionWindowPlan> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                RangedVerbEmissionWindowPlan window = source[i];
                if (window != null)
                {
                    target.Add(window);
                }
            }
        }

        /// <summary>
        /// 追加来源结果标识，保持去重。
        /// </summary>
        private static void AppendSourceResultIds(List<string> target, IReadOnlyList<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string resultId = source[i];
                if (!string.IsNullOrWhiteSpace(resultId) && !target.Contains(resultId))
                {
                    target.Add(resultId);
                }
            }
        }

        /// <summary>
        /// 统计窗口集合按上游真值预期应落地的 emit 总量。
        /// </summary>
        private static int CountExpectedEmits(IReadOnlyList<RangedVerbEmissionWindowPlan> windows)
        {
            if (windows == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < windows.Count; i++)
            {
                RangedVerbEmissionWindowPlan window = windows[i];
                if (window == null)
                {
                    continue;
                }

                total += window.ExpectedEmitCount > 0
                    ? window.ExpectedEmitCount
                    : window.ProjectilePlans != null ? window.ProjectilePlans.Count : 0;
            }

            return total;
        }
    }
}
