namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 当前 Impact（命中结算）对本次投射物伤害层的处置方式。
    /// 额外效果是否执行不由此枚举决定，二者保持独立。
    /// </summary>
    public enum DamageDisposition
    {
        /// <summary>
        /// 保留原版基线和模块伤害。
        /// </summary>
        Preserve,

        /// <summary>
        /// 只抑制原版基线伤害。
        /// </summary>
        SuppressBaselineImpact,

        /// <summary>
        /// 抑制模块提交的直接伤害和额外伤害。
        /// </summary>
        SuppressModuleExtraDamage,

        /// <summary>
        /// 抑制本次投射物 Impact 产生的全部伤害。
        /// </summary>
        SuppressAllProjectileImpact
    }
}
