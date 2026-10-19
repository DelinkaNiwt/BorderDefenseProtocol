namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 额外效果作用于身体部位的中性模式。
    /// </summary>
    public enum ExtraEffectPartTargetMode
    {
        /// <summary>
        /// 不限定身体部位，沿用效果执行器的原有目标语义。
        /// </summary>
        None,

        /// <summary>
        /// 作用于本次直接命中上下文携带的身体部位。
        /// </summary>
        ImpactPart
    }
}
