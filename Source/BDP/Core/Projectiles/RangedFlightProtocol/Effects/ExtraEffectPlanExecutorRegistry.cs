using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Support.Diagnostics;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Effects
{
    /// <summary>
    /// 额外效果执行器注册表。
    /// Content 通过它接入具体 Hediff 或其它业务效果，Core 不引用 Content。
    /// </summary>
    public static class ExtraEffectPlanExecutorRegistry
    {
        /// <summary>
        /// 当前已注册的执行器列表。
        /// </summary>
        private static readonly List<IExtraEffectPlanExecutor> Executors = new List<IExtraEffectPlanExecutor>();

        /// <summary>
        /// 注册一个效果执行器；同一 EffectKind 只保留首次注册者。
        /// </summary>
        public static bool TryRegister(IExtraEffectPlanExecutor executor)
        {
            if (executor == null || string.IsNullOrWhiteSpace(executor.EffectKind))
            {
                return false;
            }

            for (int index = 0; index < Executors.Count; index++)
            {
                if (Executors[index] != null && Executors[index].EffectKind == executor.EffectKind)
                {
                    return false;
                }
            }

            Executors.Add(executor);
            return true;
        }

        /// <summary>
        /// 尝试执行一条额外效果计划。
        /// </summary>
        public static bool TryExecute(ExtraEffectPlan effectPlan, ExtraEffectExecutionContext context)
        {
            if (effectPlan == null || context == null || string.IsNullOrWhiteSpace(effectPlan.EffectKind))
            {
                return false;
            }

            for (int index = 0; index < Executors.Count; index++)
            {
                IExtraEffectPlanExecutor executor = Executors[index];
                if (executor == null || executor.EffectKind != effectPlan.EffectKind)
                {
                    continue;
                }

                return executor.TryExecute(effectPlan, context);
            }

            BdpDiagnostics.Once(
                "ranged_extra_effect.unregistered." + effectPlan.EffectKind,
                "额外效果没有注册执行器，已跳过。effectKind=" + effectPlan.EffectKind);
            return false;
        }
    }
}
