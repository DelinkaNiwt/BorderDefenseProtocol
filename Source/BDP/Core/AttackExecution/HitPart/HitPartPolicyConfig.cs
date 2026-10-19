namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 可选命中部位策略的中性配置。
    /// Core 只保存路由键和回退参数，不理解策略的业务含义。
    /// </summary>
    public sealed class HitPartPolicyConfig
    {
        /// <summary>
        /// 由业务层解释的策略键。
        /// </summary>
        public string PolicyKey;

        /// <summary>
        /// 策略生效概率，范围由业务层按需要规范化。
        /// </summary>
        public float Chance = 1f;

        /// <summary>
        /// 创建当前配置的独立快照。
        /// </summary>
        public HitPartPolicyConfig Clone()
        {
            return new HitPartPolicyConfig
            {
                PolicyKey = PolicyKey,
                Chance = Chance
            };
        }
    }
}
