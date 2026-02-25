namespace BDP.Trigger
{
    /// <summary>
    /// 速度修饰数据包——每tick决定"飞多快"。
    /// 模块通过修改SpeedMultiplier来加速/减速弹道。
    /// </summary>
    public struct SpeedContext
    {
        // ── 输入（只读）──
        /// <summary>基础飞行速度（def.projectile.speed）。</summary>
        public readonly float BaseSpeed;

        // ── 可修改 ──
        /// <summary>速度乘数（默认1.0，模块可叠加修改）。</summary>
        public float SpeedMultiplier;

        public SpeedContext(float baseSpeed)
        {
            BaseSpeed = baseSpeed;
            SpeedMultiplier = 1f;
        }
    }

    /// <summary>
    /// 速度修饰管线接口——修改飞行速度（加速/减速）。
    /// 执行顺序：管线第2阶段（PathResolver之后）。
    /// </summary>
    public interface IBDPSpeedModifier
    {
        /// <summary>修改速度乘数。</summary>
        void ModifySpeed(Bullet_BDP host, ref SpeedContext ctx);
    }
}
