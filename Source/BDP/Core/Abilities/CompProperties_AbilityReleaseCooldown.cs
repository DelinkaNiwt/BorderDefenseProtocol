using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 释放冷却的通用配置。
    /// 冷却键由业务 AbilityDef 声明，Core 只负责按键保存和恢复冷却状态。
    /// </summary>
    public sealed class CompProperties_AbilityReleaseCooldown : CompProperties_AbilityEffect
    {
        /// <summary>
        /// 当前 Ability 共享的冷却键。
        /// 同一 Pawn 使用同一冷却键的 Ability 共享一份来源级冷却。
        /// </summary>
        public string cooldownKey;

        /// <summary>
        /// 绑定通用 Ability 释放冷却效果组件。
        /// </summary>
        public CompProperties_AbilityReleaseCooldown()
        {
            compClass = typeof(CompAbilityEffect_AbilityReleaseCooldown);
        }
    }
}
