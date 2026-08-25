using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// BDP 表达能力的 Trion（触力能）成本提交器。
    /// 它只复用现有成本组件，不决定具体 Ability Verb 的施放流程。
    /// </summary>
    public static class BdpAbilityTrionCostCommitter
    {
        /// <summary>
        /// 遍历指定能力的全部 BDP Trion 成本组件并正式提交。
        /// 没有成本组件时保持成功，因此普通短距跳跃继续免费。
        /// </summary>
        public static bool TryCommit(Ability ability)
        {
            if (ability?.EffectComps == null)
            {
                return true;
            }

            for (int index = 0; index < ability.EffectComps.Count; index++)
            {
                if (ability.EffectComps[index] is CompAbilityEffect_BdpTrionCost trionCostComp
                    && !trionCostComp.TryCommitCastCost())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
