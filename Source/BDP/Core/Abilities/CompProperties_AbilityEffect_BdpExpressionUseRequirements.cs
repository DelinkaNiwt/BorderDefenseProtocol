using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// 把 AbilityDef 接到表达 Combo 使用条件适配器。
    /// 具体条件只从 ComboDef 读取，本配置不重复保存业务门槛。
    /// </summary>
    public sealed class CompProperties_AbilityEffect_BdpExpressionUseRequirements
        : CompProperties_AbilityEffect
    {
        /// <summary>绑定正式的表达使用条件组件类型。</summary>
        public CompProperties_AbilityEffect_BdpExpressionUseRequirements()
        {
            compClass = typeof(CompAbilityEffect_BdpExpressionUseRequirements);
        }
    }
}
