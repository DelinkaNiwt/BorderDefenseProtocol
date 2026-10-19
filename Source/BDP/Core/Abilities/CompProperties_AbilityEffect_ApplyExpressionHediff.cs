using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 一次性应用表达宿主 Hediff 的通用配置。
    /// Hediff 的持续时间由目标 HediffDef 自己声明，不由 Ability 组件复制。
    /// </summary>
    public sealed class CompProperties_AbilityEffect_ApplyExpressionHediff : CompProperties_AbilityEffect
    {
        /// <summary>
        /// 要应用的表达宿主 HediffDef 名称。
        /// </summary>
        public string hediffDefName;

        /// <summary>
        /// 绑定通用一次性 Hediff 应用效果组件。
        /// </summary>
        public CompProperties_AbilityEffect_ApplyExpressionHediff()
        {
            compClass = typeof(CompAbilityEffect_ApplyExpressionHediff);
        }
    }
}
