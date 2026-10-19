namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 伤害计划的命中部位解析策略。
    /// Core 只表达执行策略，不解释具体业务上的“要害”含义。
    /// </summary>
    public enum HitPartResolutionMode
    {
        /// <summary>
        /// 不继承当前 Impact 已解析的命中部位，交给原版重新选择。
        /// </summary>
        Independent,

        /// <summary>
        /// 继承当前 Impact 已解析的命中部位；没有部位时回退原版选择。
        /// </summary>
        InheritImpactHitPart
    }
}
