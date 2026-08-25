namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 额外效果取得目标的中性来源。
    /// </summary>
    public enum ExtraEffectTargetScope
    {
        /// <summary>
        /// 订阅攻击生产者实际产生的每个攻击目标事件。
        /// </summary>
        AttackTargetEvents,

        /// <summary>
        /// 只使用原版直接命中的 Thing。
        /// </summary>
        DirectHitThing,

        /// <summary>
        /// 使用原版爆炸逐目标链路取得的全部 Thing。
        /// </summary>
        VanillaExplosionAffectedThings,

        /// <summary>
        /// 使用原版爆炸逐目标链路取得的 Pawn。
        /// </summary>
        VanillaExplosionAffectedPawns
    }
}
