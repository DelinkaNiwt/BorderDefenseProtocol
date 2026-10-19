namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 额外效果相对于投射物伤害的执行时序。
    /// </summary>
    public enum ExtraEffectExecutionTiming
    {
        /// <summary>
        /// 伤害结算完成后执行，保持原有默认行为。
        /// </summary>
        AfterDamage = 0,

        /// <summary>
        /// 伤害结算前执行，用于必须在目标被摧毁前生成的命中特效。
        /// </summary>
        BeforeDamage = 1
    }
}
