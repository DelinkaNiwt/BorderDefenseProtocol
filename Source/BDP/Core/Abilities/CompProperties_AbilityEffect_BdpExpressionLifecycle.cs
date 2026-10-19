using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>显式选择保留原版能力宿主；未配置的能力不改变回收规则。</summary>
    public sealed class CompProperties_AbilityEffect_BdpExpressionLifecycle : CompProperties_AbilityEffect
    {
        /// <summary>来源停用时保留次数与原生恢复计时。</summary>
        public bool retainWhenInactive;

        /// <summary>绑定来源资格组件。</summary>
        public CompProperties_AbilityEffect_BdpExpressionLifecycle()
        {
            compClass = typeof(CompAbilityEffect_BdpExpressionLifecycle);
        }
    }
}
