namespace BDP.Trigger
{
    /// <summary>发射时速度修正上下文。输入字段 readonly，输出字段可修改。</summary>
    public struct SpeedContext
    {
        /// <summary>初始速度倍率（来自 CompFireMode.Speed）。</summary>
        public readonly float BaseSpeedMult;
        /// <summary>最终速度倍率（模块可修改）。</summary>
        public float SpeedMult;

        public SpeedContext(float speedMult)
        {
            BaseSpeedMult = speedMult;
            SpeedMult     = speedMult;
        }
    }

    /// <summary>
    /// 发射时速度修正管线接口。
    /// 在 Projectile.Launch Postfix 中调用（一次性，非 per-tick）。
    /// 实现此接口的模块可在 ReinitFlight 执行前修改速度倍率。
    /// </summary>
    public interface IBDPSpeedModifier
    {
        void ModifySpeed(Bullet_BDP bullet, ref SpeedContext ctx);
    }
}
