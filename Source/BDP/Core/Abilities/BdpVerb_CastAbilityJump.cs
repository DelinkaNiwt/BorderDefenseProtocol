using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// BDP 表达系统的跳跃 Ability Verb（能力动词）基类。
    /// 它只在原版跳跃执行前提交表达结果声明的 Trion 成本。
    /// </summary>
    public class BdpVerb_CastAbilityJump : Verb_CastAbilityJump, IBdpExpressionAbilityVerb
    {
        /// <summary>
        /// 先提交 Trion 成本，成功后完整进入原版跳跃施放流程。
        /// </summary>
        protected override bool TryCastShot()
        {
            if (!BdpAbilityTrionCostCommitter.TryCommit(ability))
            {
                return false;
            }

            return base.TryCastShot();
        }
    }
}
