using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 的 Trion 施法成本配置。
    /// 它只负责把 AbilityDef 接到 BDP 扣费组件，具体成本来自表达结果绑定。
    /// </summary>
    public sealed class CompProperties_AbilityEffect_BdpTrionCost : CompProperties_AbilityEffect
    {
        /// <summary>
        /// 绑定到正式的 Trion 施法成本组件类型。
        /// </summary>
        public CompProperties_AbilityEffect_BdpTrionCost()
        {
            compClass = typeof(CompAbilityEffect_BdpTrionCost);
        }
    }
}
