namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 原版范围爆炸表现策略。
    /// 空值或未启用覆盖时保持原版视觉、音效和屏幕震动。
    /// </summary>
    public sealed class ExplosionPresentationPolicy
    {
        /// <summary>
        /// 是否关闭原版爆炸视觉。
        /// </summary>
        public bool SuppressVanillaVisualEffects { get; set; }

        /// <summary>
        /// 是否关闭原版爆炸音效。
        /// </summary>
        public bool SuppressVanillaSoundEffects { get; set; }

        /// <summary>
        /// 是否覆盖原版屏幕震动强度。
        /// </summary>
        public bool OverrideScreenShakeFactor { get; set; }

        /// <summary>
        /// 覆盖后的屏幕震动强度。
        /// </summary>
        public float ScreenShakeFactor { get; set; }

        /// <summary>
        /// 复制当前表现策略。
        /// </summary>
        public ExplosionPresentationPolicy Clone()
        {
            return new ExplosionPresentationPolicy
            {
                SuppressVanillaVisualEffects = SuppressVanillaVisualEffects,
                SuppressVanillaSoundEffects = SuppressVanillaSoundEffects,
                OverrideScreenShakeFactor = OverrideScreenShakeFactor,
                ScreenShakeFactor = ScreenShakeFactor
            };
        }
    }
}
