using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 远程芯片配置。
    /// 定义远程攻击的特殊机制（齐射散布、引导飞行、穿透等）。
    /// </summary>
    public class RangedConfig
    {
        /// <summary>
        /// 齐射时每发子弹射出起点的随机偏移半径（格）。
        /// 0 = 无偏移（所有子弹从同一点射出）
        /// 0.3 = 轻微散布（适合精确武器）
        /// 0.6 = 明显散布（适合霰弹类武器）
        /// 仅在burstShotCount > 1时有效。
        /// </summary>
        public float volleySpreadRadius = 0f;

        /// <summary>
        /// 引导飞行配置。
        /// null = 不支持引导飞行（直线弹道）
        /// 非null = 支持变化弹引导飞行模式
        /// </summary>
        public GuidedConfig guided;

        /// <summary>
        /// 穿体穿透力初始值。
        /// 0 = 不穿透（命中目标后停止）
        /// >0 = 可穿透，每次穿透后递减，降至0时停止
        /// 区别于护甲穿透（armorPenetration）——此值决定子弹能否穿过目标继续飞行。
        /// </summary>
        public float passthroughPower = 0f;
    }
}
