namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 伤害被模块拦截后，是否补回原版 Pawn（人形单位）受击反馈。
    /// </summary>
    public enum ImpactHitFeedbackMode
    {
        /// <summary>
        /// 不补回受击反馈。
        /// </summary>
        None,

        /// <summary>
        /// 补回原版 Pawn Drawer（角色绘制器）受击反馈和子弹僵直。
        /// </summary>
        VanillaPawn
    }
}
