using BDP.Core.Projectiles.RangedFlightProtocol.Model;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Effects
{
    /// <summary>
    /// 额外效果计划执行器协议。
    /// Core 只按 EffectKind（效果种类键）分发，不解释业务效果。
    /// </summary>
    public interface IExtraEffectPlanExecutor
    {
        /// <summary>
        /// 当前执行器承接的效果种类键。
        /// </summary>
        string EffectKind { get; }

        /// <summary>
        /// 尝试执行一条额外效果计划。
        /// </summary>
        bool TryExecute(ExtraEffectPlan effectPlan, ExtraEffectExecutionContext context);
    }
}
