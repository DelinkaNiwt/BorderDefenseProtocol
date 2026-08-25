namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 武器视觉可读取的原版攻击动作阶段。
    /// 该枚举只描述共性时序，不规定任何具体武器应如何显示。
    /// </summary>
    public enum WeaponVisualActionStage
    {
        /// <summary>
        /// 当前来源未处于一次受认可的攻击动作中。
        /// </summary>
        Idle,

        /// <summary>
        /// 原版瞄准预热阶段。
        /// </summary>
        Warmup,

        /// <summary>
        /// 正式宿主正在执行一轮射击；连发内部间隔仍属于此阶段。
        /// </summary>
        Firing,

        /// <summary>
        /// 一轮射击结束后的原版最终冷却阶段。
        /// </summary>
        Cooldown
    }
}
