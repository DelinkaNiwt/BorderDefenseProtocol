namespace BDP.Trigger
{
    /// <summary>
    /// 发射模式枚举，定义武器芯片的弹药发射方式。
    /// </summary>
    public enum FiringPattern
    {
        /// <summary>
        /// 逐发模式：由引擎的 burst 机制驱动，每发子弹之间有时间间隔。
        /// </summary>
        Sequential,

        /// <summary>
        /// 齐射模式：在单次 TryCastShot 调用内循环瞬发所有子弹，无间隔。
        /// </summary>
        Simultaneous
    }
}
