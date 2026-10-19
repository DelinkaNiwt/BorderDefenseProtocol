using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 按钮切换表达 Hediff 的通用配置。
    /// 业务内容只需在 AbilityDef 中声明目标 HediffDefName，Core 不绑定具体状态名称。
    /// </summary>
    public sealed class CompProperties_AbilityEffect_ToggleExpressionHediff : CompProperties_AbilityEffect
    {
        /// <summary>
        /// 要切换的表达宿主 HediffDef 名称。
        /// </summary>
        public string hediffDefName;

        /// <summary>
        /// 绑定通用 Hediff 切换效果组件。
        /// </summary>
        public CompProperties_AbilityEffect_ToggleExpressionHediff()
        {
            compClass = typeof(CompAbilityEffect_ToggleExpressionHediff);
        }
    }
}
